namespace HygiaTrade.API.Services;

public interface IDatabaseBackupOperationLock
{
    Task<IAsyncDisposable> AcquireAsync(
        CancellationToken cancellationToken);
}

public sealed class DatabaseBackupOperationLock
    : IDatabaseBackupOperationLock,
      IDisposable
{
    private readonly SemaphoreSlim semaphore =
        new(1, 1);

    public async Task<IAsyncDisposable> AcquireAsync(
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        return new Lease(semaphore);
    }

    public void Dispose() => semaphore.Dispose();

    private sealed class Lease(
        SemaphoreSlim semaphore)
        : IAsyncDisposable
    {
        private bool released;

        public ValueTask DisposeAsync()
        {
            if (!released)
            {
                semaphore.Release();
                released = true;
            }

            return ValueTask.CompletedTask;
        }
    }
}
