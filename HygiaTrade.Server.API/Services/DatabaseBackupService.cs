namespace HygiaTrade.API.Services;

public sealed record DatabaseBackupArtifact(
    string FilePath,
    string FileName);

public sealed class DatabaseBackupException
    : InvalidOperationException
{
    public DatabaseBackupException(string message)
        : base(message)
    {
    }

    public DatabaseBackupException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}

public interface IDatabaseBackupService
{
    Task<DatabaseBackupArtifact> CreateBackupAsync(
        CancellationToken cancellationToken = default);

    Task RestoreBackupAsync(
        Stream archive,
        CancellationToken cancellationToken = default);
}

public sealed class DatabaseBackupService(
    IPostgresBackupTool postgresTool,
    IDatabaseBackupArchiveValidator archiveValidator,
    IDatabaseBackupFileStore fileStore,
    IDatabaseBackupOperationLock operationLock,
    ILogger<DatabaseBackupService> logger)
    : IDatabaseBackupService
{
    public async Task<DatabaseBackupArtifact> CreateBackupAsync(
        CancellationToken cancellationToken = default)
    {
        await using IAsyncDisposable operationLease =
            await operationLock.AcquireAsync(cancellationToken);

        string? backupPath = null;

        try
        {
            backupPath =
                fileStore.CreateTemporaryPath("dump");

            await postgresTool.CreateBackupAsync(
                backupPath,
                cancellationToken);

            await archiveValidator.ValidateHeaderAsync(
                backupPath,
                cancellationToken);

            long backupSize =
                fileStore.GetLength(backupPath);

            if (backupSize == 0)
            {
                throw new DatabaseBackupException(
                    "PostgreSQL reported a successful backup, but the generated archive is empty.");
            }

            string downloadName =
                $"higiatrade-full-database-{DateTime.UtcNow:yyyyMMdd-HHmmss}Z.dump";

            logger.LogInformation(
                "Full PostgreSQL backup created successfully ({BackupSize} bytes).",
                backupSize);

            return new DatabaseBackupArtifact(
                backupPath,
                downloadName);
        }
        catch (InvalidDataException ex)
        {
            DeleteIfCreated(backupPath);

            throw new DatabaseBackupException(
                "The generated PostgreSQL backup archive failed validation.",
                ex);
        }
        catch
        {
            DeleteIfCreated(backupPath);
            throw;
        }
    }

    public async Task RestoreBackupAsync(
        Stream archive,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archive);

        await using IAsyncDisposable operationLease =
            await operationLock.AcquireAsync(cancellationToken);

        string? uploadedPath = null;

        try
        {
            uploadedPath =
                fileStore.CreateTemporaryPath("dump");

            await fileStore.CopyToFileAsync(
                archive,
                uploadedPath,
                cancellationToken);

            long uploadedSize =
                fileStore.GetLength(uploadedPath);

            if (uploadedSize == 0)
            {
                throw new InvalidDataException(
                    "The uploaded backup archive is empty.");
            }

            await archiveValidator.ValidateHeaderAsync(
                uploadedPath,
                cancellationToken);

            try
            {
                await postgresTool.ValidateArchiveAsync(
                    uploadedPath,
                    cancellationToken);
            }
            catch (DatabaseBackupException ex)
            {
                throw new InvalidDataException(
                    "The uploaded file is not a valid PostgreSQL custom-format backup archive.",
                    ex);
            }

            await postgresTool.RestoreBackupAsync(
                uploadedPath,
                cancellationToken);

            logger.LogWarning(
                "Full PostgreSQL database restore completed successfully from an uploaded archive ({BackupSize} bytes).",
                uploadedSize);
        }
        finally
        {
            DeleteIfCreated(uploadedPath);
        }
    }

    private void DeleteIfCreated(string? path)
    {
        if (path is not null)
        {
            fileStore.TryDelete(path);
        }
    }
}
