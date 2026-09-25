using HygiaTrade.API.Models;
using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class AdminReportBuilderTests
{
    private readonly AdminReportBuilder builder = new();

    [Fact]
    public void Build_AggregatesSalesInventorySummaryAndCategories()
    {
        Guid productA = Guid.NewGuid();
        Guid productB = Guid.NewGuid();
        Guid order1 = Guid.NewGuid();
        Guid order2 = Guid.NewGuid();

        var request =
            new ReportsRequest(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31),
                5,
                new DateTime(
                    2026,
                    1,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc),
                new DateTime(
                    2026,
                    2,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc));

        IReadOnlyList<ReportProductData> products =
        [
            new(productA, "Alpha", 0, "B"),
            new(productB, "Beta", 5, "A"),
            new(Guid.NewGuid(), "Gamma", 20, "a")
        ];

        IReadOnlyList<ReportOrderData> orders =
        [
            new(order1, 20m),
            new(order2, 35m)
        ];

        IReadOnlyList<ReportOrderItemData> items =
        [
            new(productA, "Alpha", 2, 20m, order1),
            new(productA, "Alpha", 3, 30m, order2),
            new(productB, "Beta", 1, 5m, order2)
        ];

        IReadOnlyList<StockEntryRow> stockEntries =
        [
            new(
                Guid.NewGuid(),
                productA,
                "Alpha",
                "B",
                7,
                "INV-1",
                request.FromUtc),
            new(
                Guid.NewGuid(),
                productA,
                "Alpha",
                "B",
                2,
                "INV-2",
                request.FromUtc),
            new(
                Guid.NewGuid(),
                productB,
                "Beta",
                "A",
                4,
                "INV-3",
                request.FromUtc)
        ];

        AdminReportResponse result =
            builder.Build(
                request,
                products,
                orders,
                items,
                stockEntries);

        Assert.Equal(request.From, result.From);
        Assert.Equal(request.To, result.To);
        Assert.Equal(5, result.LowStockThreshold);

        Assert.Equal(3, result.Summary.TotalProducts);
        Assert.Equal(25UL, result.Summary.TotalUnitsInStock);
        Assert.Equal(1, result.Summary.LowStockProducts);
        Assert.Equal(1, result.Summary.OutOfStockProducts);
        Assert.Equal(13, result.Summary.ReceivedUnits);
        Assert.Equal(6, result.Summary.SoldUnits);
        Assert.Equal(2, result.Summary.TotalOrders);
        Assert.Equal(55m, result.Summary.Revenue);

        Assert.Equal(2, result.Categories.Count);
        Assert.Contains("A", result.Categories);
        Assert.Contains("B", result.Categories);

        Assert.Collection(
            result.Sales,
            alpha =>
            {
                Assert.Equal(productA, alpha.ProductId);
                Assert.Equal("Alpha", alpha.ProductName);
                Assert.Equal(5, alpha.SoldQuantity);
                Assert.Equal(50m, alpha.Revenue);
                Assert.Equal(2, alpha.OrderCount);
            },
            beta =>
            {
                Assert.Equal(productB, beta.ProductId);
                Assert.Equal(1, beta.SoldQuantity);
                Assert.Equal(5m, beta.Revenue);
                Assert.Equal(1, beta.OrderCount);
            });

        Assert.Collection(
            result.Inventory,
            alpha =>
            {
                Assert.Equal(productA, alpha.ProductId);
                Assert.Equal(0u, alpha.CurrentQuantity);
                Assert.Equal(9, alpha.ReceivedQuantity);
                Assert.Equal(5, alpha.SoldQuantity);
                Assert.Equal(4, alpha.NetMovement);
            },
            beta =>
            {
                Assert.Equal(productB, beta.ProductId);
                Assert.Equal(5u, beta.CurrentQuantity);
                Assert.Equal(4, beta.ReceivedQuantity);
                Assert.Equal(1, beta.SoldQuantity);
                Assert.Equal(3, beta.NetMovement);
            },
            gamma =>
            {
                Assert.Equal("Gamma", gamma.ProductName);
                Assert.Equal(20u, gamma.CurrentQuantity);
                Assert.Equal(0, gamma.ReceivedQuantity);
                Assert.Equal(0, gamma.SoldQuantity);
                Assert.Equal(0, gamma.NetMovement);
            });

        Assert.Same(
            stockEntries,
            result.StockEntries);
    }

    [Fact]
    public void Build_SortsEqualSalesByProductNameAndCountsDistinctOrders()
    {
        Guid orderId = Guid.NewGuid();

        IReadOnlyList<ReportOrderItemData> items =
        [
            new(Guid.NewGuid(), "Zulu", 2, 10m, orderId),
            new(Guid.NewGuid(), "Alpha", 2, 20m, orderId)
        ];

        AdminReportResponse result =
            builder.Build(
                Request(),
                [],
                [new ReportOrderData(orderId, 30m)],
                items,
                []);

        Assert.Equal("Alpha", result.Sales[0].ProductName);
        Assert.Equal("Zulu", result.Sales[1].ProductName);
        Assert.All(
            result.Sales,
            row => Assert.Equal(1, row.OrderCount));
    }

    [Fact]
    public void Build_HandlesEmptyInput()
    {
        AdminReportResponse result =
            builder.Build(
                Request(),
                [],
                [],
                [],
                []);

        Assert.Empty(result.Categories);
        Assert.Empty(result.Inventory);
        Assert.Empty(result.Sales);
        Assert.Empty(result.StockEntries);

        Assert.Equal(
            new ReportSummary(
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0m),
            result.Summary);
    }

    private static ReportsRequest Request() =>
        new(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            5,
            new DateTime(
                2026,
                1,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc),
            new DateTime(
                2026,
                2,
                1,
                0,
                0,
                0,
                DateTimeKind.Utc));
}
