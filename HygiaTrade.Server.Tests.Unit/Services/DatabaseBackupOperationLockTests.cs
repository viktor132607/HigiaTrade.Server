using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class DatabaseBackupOperationLockTests
{
    [Fact]
    public async Task AcquireAsync_SerializesOperationsAndReleaseAllowsNext()
    {
        using var operationLock =
            new DatabaseBackupOperationLock();

        IAsyncDisposable first =
            await operationLock.AcquireAsync(
                CancellationToken.None);

        using var cancellation =
            new CancellationTokenSource(
                TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
            {
                await operationLock.AcquireAsync(
                    cancellation.Token);
            });

        await first.DisposeAsync();

        IAsyncDisposable second =
            await operationLock.AcquireAsync(
                CancellationToken.None);

        await second.DisposeAsync();
    }
}
