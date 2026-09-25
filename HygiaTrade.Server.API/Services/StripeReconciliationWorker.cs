using HygiaTrade.Domain.Payments;
namespace HygiaTrade.API.Services;

// Also confirms payments when a browser is closed or webhook delivery is delayed.
public sealed class StripeReconciliationWorker(IServiceScopeFactory scopeFactory, ILogger<StripeReconciliationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<StripeCheckoutService>().ReconcileAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Stripe reconciliation did not complete; retrying on the next pass."); }
        }
    }
}
