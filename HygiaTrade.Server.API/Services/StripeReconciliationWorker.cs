using HygiaTrade.Domain.Payments;

namespace HygiaTrade.API.Services;

// Also confirms payments when a browser is closed or webhook delivery is delayed.
// A PostgreSQL advisory lock ensures one active reconciliation cycle across all API instances.
public sealed class StripeReconciliationWorker(
    IServiceScopeFactory scopeFactory,
    PostgresAdvisoryLock advisoryLock,
    ILogger<StripeReconciliationWorker> logger) : BackgroundService
{
    private const long ReconciliationLockKey = 637410202;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using IAsyncDisposable? lease =
                    await advisoryLock.TryAcquireAsync(ReconciliationLockKey, stoppingToken);

                if (lease is null)
                {
                    logger.LogDebug("Skipping Stripe reconciliation because another instance owns the distributed lock.");
                    continue;
                }

                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<StripeCheckoutService>().ReconcileAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Stripe reconciliation did not complete; retrying on the next pass.");
            }
        }
    }
}
