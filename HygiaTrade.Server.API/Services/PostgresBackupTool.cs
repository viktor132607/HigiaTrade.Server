using Npgsql;

namespace HygiaTrade.API.Services;

public interface IPostgresBackupTool
{
    Task CreateBackupAsync(
        string backupPath,
        CancellationToken cancellationToken);

    Task ValidateArchiveAsync(
        string backupPath,
        CancellationToken cancellationToken);

    Task RestoreBackupAsync(
        string backupPath,
        CancellationToken cancellationToken);
}

public sealed class PostgresBackupTool(
    DatabaseBackupConnection connection,
    IPostgresProcessRunner processRunner)
    : IPostgresBackupTool
{
    public Task CreateBackupAsync(
        string backupPath,
        CancellationToken cancellationToken) =>
        processRunner.RunAsync(
            new PostgresProcessRequest(
                "pg_dump",
                [
                    "--no-password",
                    "--format=custom",
                    "--compress=9",
                    "--file",
                    backupPath,
                    "--dbname",
                    connection.Database
                ],
                connection.CreateEnvironment(),
                "create the database backup"),
            cancellationToken);

    public Task ValidateArchiveAsync(
        string backupPath,
        CancellationToken cancellationToken) =>
        processRunner.RunAsync(
            new PostgresProcessRequest(
                "pg_restore",
                ["--list", backupPath],
                null,
                "validate the uploaded database backup"),
            cancellationToken);

    public async Task RestoreBackupAsync(
        string backupPath,
        CancellationToken cancellationToken)
    {
        NpgsqlConnection.ClearAllPools();

        await processRunner.RunAsync(
            new PostgresProcessRequest(
                "pg_restore",
                [
                    "--no-password",
                    "--clean",
                    "--if-exists",
                    "--no-owner",
                    "--no-privileges",
                    "--single-transaction",
                    "--exit-on-error",
                    "--dbname",
                    connection.Database,
                    backupPath
                ],
                connection.CreateEnvironment(),
                "restore the database backup"),
            cancellationToken);

        NpgsqlConnection.ClearAllPools();
    }
}
