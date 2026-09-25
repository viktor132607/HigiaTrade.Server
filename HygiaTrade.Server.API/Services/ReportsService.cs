using HygiaTrade.API.Models;

namespace HygiaTrade.API.Services;

public interface IReportsService
{
    Task<AdminReportResponse> GetAsync(
        DateOnly? from,
        DateOnly? to,
        int lowStockThreshold,
        CancellationToken cancellationToken = default);
}

public sealed class ReportsServiceException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class ReportsService(
    IReportsRequestNormalizer requestNormalizer,
    IReportsDataSource dataSource,
    IStockEntryReportReader stockEntryReader,
    IAdminReportBuilder reportBuilder)
    : IReportsService
{
    public async Task<AdminReportResponse> GetAsync(
        DateOnly? from,
        DateOnly? to,
        int lowStockThreshold,
        CancellationToken cancellationToken = default)
    {
        ReportsRequest request =
            requestNormalizer.Normalize(
                from,
                to,
                lowStockThreshold,
                DateTime.UtcNow);

        IReadOnlyList<ReportProductData> products =
            await dataSource.GetProductsAsync(
                cancellationToken);

        IReadOnlyList<ReportOrderData> orders =
            await dataSource.GetOrdersAsync(
                request.FromUtc,
                request.ToExclusiveUtc,
                cancellationToken);

        Guid[] orderIds =
            orders.Select(order => order.Id).ToArray();

        IReadOnlyList<ReportOrderItemData> orderItems =
            await dataSource.GetOrderItemsAsync(
                orderIds,
                cancellationToken);

        IReadOnlyList<StockEntryRow> stockEntries =
            await stockEntryReader.ReadSafeAsync(
                request.FromUtc,
                request.ToExclusiveUtc,
                cancellationToken);

        return reportBuilder.Build(
            request,
            products,
            orders,
            orderItems,
            stockEntries);
    }
}
