using HygiaTrade.API.Models;
using HygiaTrade.API.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class ReportsServiceTests
{
    private readonly Mock<IReportsRequestNormalizer> normalizer = new();
    private readonly Mock<IReportsDataSource> dataSource = new();
    private readonly Mock<IStockEntryReportReader> stockEntries = new();
    private readonly Mock<IAdminReportBuilder> builder = new();

    private ReportsService CreateService() =>
        new(
            normalizer.Object,
            dataSource.Object,
            stockEntries.Object,
            builder.Object);

    [Fact]
    public async Task GetAsync_LoadsAllSourcesAndBuildsReport()
    {
        DateOnly from = new(2026, 1, 1);
        DateOnly to = new(2026, 1, 31);
        DateTime fromUtc =
            new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime toExclusiveUtc =
            new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        var request =
            new ReportsRequest(
                from,
                to,
                5,
                fromUtc,
                toExclusiveUtc);

        Guid orderId = Guid.NewGuid();

        IReadOnlyList<ReportProductData> products =
        [
            new(
                Guid.NewGuid(),
                "Product",
                3,
                "Category")
        ];

        IReadOnlyList<ReportOrderData> orders =
        [
            new(orderId, 20m)
        ];

        IReadOnlyList<ReportOrderItemData> items =
        [
            new(
                products[0].Id,
                "Product",
                2,
                20m,
                orderId)
        ];

        IReadOnlyList<StockEntryRow> entries =
        [
            new(
                Guid.NewGuid(),
                products[0].Id,
                "Product",
                "Category",
                5,
                "INV-1",
                fromUtc)
        ];

        AdminReportResponse expected =
            new(
                from,
                to,
                5,
                new ReportSummary(
                    1,
                    3,
                    1,
                    0,
                    5,
                    2,
                    1,
                    20m),
                ["Category"],
                [],
                [],
                entries);

        normalizer
            .Setup(x => x.Normalize(
                from,
                to,
                5,
                It.Is<DateTime>(value =>
                    value.Kind == DateTimeKind.Utc)))
            .Returns(request);

        dataSource
            .Setup(x => x.GetProductsAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        dataSource
            .Setup(x => x.GetOrdersAsync(
                fromUtc,
                toExclusiveUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        dataSource
            .Setup(x => x.GetOrderItemsAsync(
                It.Is<IReadOnlyCollection<Guid>>(
                    ids =>
                        ids.SequenceEqual(
                            new[] { orderId })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        stockEntries
            .Setup(x => x.ReadSafeAsync(
                fromUtc,
                toExclusiveUtc,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        builder
            .Setup(x => x.Build(
                request,
                products,
                orders,
                items,
                entries))
            .Returns(expected);

        AdminReportResponse result =
            await CreateService().GetAsync(
                from,
                to,
                5,
                CancellationToken.None);

        Assert.Same(expected, result);
    }
}
