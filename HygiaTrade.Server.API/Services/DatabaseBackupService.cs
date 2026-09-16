using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Npgsql;

namespace HygiaTrade.API.Services;

public sealed record DatabaseBackupArtifact(string FilePath, string FileName);

public sealed class DatabaseBackupService
{
    private const int CopyBufferSize = 128 * 1024;
    private static readonly byte[] PgDumpMagic = Encoding.ASCII.GetBytes("PGDMP");

    private readonly NpgsqlConnectionStringBuilder connection;
    private readonly ILogger<DatabaseBackupService> logger;
    private readonly SemaphoreSlim operationLock = new(1, 1);

    public DatabaseBackupService(
        string connectionString,
        ILogger<DatabaseBackupService> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(logger);

        connection = new NpgsqlConnectionStringBuilder(connectionString);
        this.logger = logger;

        if (string.IsNullOrWhiteSpace(connection.Host) ||
            string.IsNullOrWhiteSpace(connection.Database) ||
            string.IsNullOrWhiteSpace(connection.Username))
        {
            throw new InvalidOperationException(
                "Database backup requires PostgreSQL host, database, and username configuration.");
        }
    }

    public async Task<DatabaseBackupArtifact> CreateBackupAsync(
        CancellationToken cancellationToken = default)
    {
        await operationLock.WaitAsync(cancellationToken);
        string? backupPath = null;

        try
        {
            backupPath = CreateTemporaryPath("dump");

            await RunPostgresToolAsync(
                "pg_dump",
                [
                    "--format=custom",
                    "--compress=9",
                    "--file",
                    backupPath,
                    connection.Database
                ],
                "create the database backup",
                cancellationToken);

            await ValidatePgDumpHeaderAsync(backupPath, cancellationToken);

            FileInfo backupFile = new(backupPath);
            if (!backupFile.Exists || backupFile.Length == 0)
            {
                throw new InvalidOperationException(
                    "PostgreSQL reported a successful backup, but the generated archive is empty.");
            }

            string downloadName =
                $"higiatrade-full-database-{DateTime.UtcNow:yyyyMMdd-HHmmss}Z.dump";

            logger.LogInformation(
                "Full PostgreSQL backup created successfully ({BackupSize} bytes).",
                backupFile.Length);

            return new DatabaseBackupArtifact(backupPath, downloadName);
        }
        catch
        {
            if (backupPath is not null)
            {
                TryDelete(backupPath);
            }

            throw;
        }
        finally
        {
            operationLock.Release();
        }
    }

    public async Task RestoreBackupAsync(
        Stream archive,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archive);

        await operationLock.WaitAsync(cancellationToken);
        string? uploadedPath = null;

        try
        {
            uploadedPath = CreateTemporaryPath("dump");

            await using (FileStream destination = new(
                uploadedPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                CopyBufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await archive.CopyToAsync(
                    destination,
                    CopyBufferSize,
                    cancellationToken);
            }

            FileInfo uploadedFile = new(uploadedPath);
            if (uploadedFile.Length == 0)
            {
                throw new InvalidDataException("The uploaded backup archive is empty.");
            }

            await ValidatePgDumpHeaderAsync(uploadedPath, cancellationToken);

            try
            {
                await RunPostgresToolAsync(
                    "pg_restore",
                    ["--list", uploadedPath],
                    "validate the uploaded database backup",
                    cancellationToken);
            }
            catch (PostgresToolException ex)
            {
                throw new InvalidDataException(
                    "The uploaded file is not a valid PostgreSQL custom-format backup archive.",
                    ex);
            }

            // Close any idle application connections before pg_restore starts replacing
            // database objects. The restore itself is executed in one transaction, so a
            // failed restore rolls back instead of leaving a half-restored database.
            NpgsqlConnection.ClearAllPools();

            await RunPostgresToolAsync(
                "pg_restore",
                [
                    "--clean",
                    "--if-exists",
                    "--no-owner",
                    "--no-privileges",
                    "--single-transaction",
                    "--exit-on-error",
                    "--dbname",
                    connection.Database,
                    uploadedPath
                ],
                "restore the database backup",
                cancellationToken);

            NpgsqlConnection.ClearAllPools();

            logger.LogWarning(
                "Full PostgreSQL database restore completed successfully from an uploaded archive ({BackupSize} bytes).",
                uploadedFile.Length);
        }
        finally
        {
            if (uploadedPath is not null)
            {
                TryDelete(uploadedPath);
            }

            operationLock.Release();
        }
    }

    private async Task RunPostgresToolAsync(
        string executable,
        IReadOnlyCollection<string> arguments,
        string operation,
        CancellationToken cancellationToken)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = executable,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        ApplyPostgresEnvironment(startInfo);

        using Process process = new() { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException(
                    $"Unable to start {executable} while trying to {operation}.");
            }
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(
                $"The PostgreSQL utility '{executable}' is not installed or is not available in PATH.",
                ex);
        }

        Task<string> standardOutputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        string standardOutput = await standardOutputTask;
        string standardError = await standardErrorTask;

        if (process.ExitCode != 0)
        {
            throw new PostgresToolException(
                executable,
                operation,
                process.ExitCode,
                standardError);
        }

        if (!string.IsNullOrWhiteSpace(standardError))
        {
            logger.LogDebug(
                "{PostgresTool} completed while trying to {Operation}: {ToolOutput}",
                executable,
                operation,
                standardError.Trim());
        }

        // pg_restore --list writes its table of contents to stdout. Reading it above is
        // intentional so the process cannot block on a full output pipe.
        _ = standardOutput;
    }

    private void ApplyPostgresEnvironment(ProcessStartInfo startInfo)
    {
        startInfo.Environment["PGHOST"] = connection.Host;
        startInfo.Environment["PGPORT"] = connection.Port.ToString();
        startInfo.Environment["PGDATABASE"] = connection.Database;
        startInfo.Environment["PGUSER"] = connection.Username;

        if (!string.IsNullOrEmpty(connection.Password))
        {
            startInfo.Environment["PGPASSWORD"] = connection.Password;
        }

        string sslMode = connection.SslMode.ToString();
        startInfo.Environment["PGSSLMODE"] = sslMode switch
        {
            "VerifyCA" => "verify-ca",
            "VerifyFull" => "verify-full",
            _ => sslMode.ToLowerInvariant()
        };
    }

    private static async Task ValidatePgDumpHeaderAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            PgDumpMagic.Length,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        byte[] header = new byte[PgDumpMagic.Length];
        int totalRead = 0;

        while (totalRead < header.Length)
        {
            int read = await stream.ReadAsync(
                header.AsMemory(totalRead, header.Length - totalRead),
                cancellationToken);

            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        if (totalRead != PgDumpMagic.Length || !header.SequenceEqual(PgDumpMagic))
        {
            throw new InvalidDataException(
                "The file is not a PostgreSQL custom-format backup archive.");
        }
    }

    private static string CreateTemporaryPath(string extension)
    {
        string fileName = $"higiatrade-db-{Guid.NewGuid():N}.{extension}";
        return Path.Combine(Path.GetTempPath(), fileName);
    }

    private static void TryDelete(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Temporary-file cleanup must never hide the original backup/restore result.
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort cancellation cleanup only.
        }
    }

    private sealed class PostgresToolException(
        string executable,
        string operation,
        int exitCode,
        string standardError)
        : InvalidOperationException(
            $"{executable} failed to {operation} with exit code {exitCode}: " +
            (string.IsNullOrWhiteSpace(standardError)
                ? "No error details were returned by PostgreSQL."
                : standardError.Trim()))
    {
    }
}
