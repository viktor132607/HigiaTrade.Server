namespace HygiaTrade.API.Services;

public sealed class DistributedDatabaseBackupOperationLock(
    PostgresAdvisoryLock advisoryLock) : IDatabaseBackupOperationLock
{
    private const long BackupLockKey = 637410201;

    public Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken) =>
        advisoryLock.AcquireAsync(BackupLockKey, cancellationToken);
}
