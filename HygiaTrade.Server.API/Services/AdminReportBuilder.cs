using HygiaTrade.API.Models;

namespace HygiaTrade.API.Services;

public interface IAdminReportBuilder
{
    AdminReportResponse Build(
        ReportsRequest request,
        IReadOnlyList<ReportProductData> products,
        IReadOnlyList<ReportOrderData> orders,
        IReadOnlyList<ReportOrderItemData> orderItems,
        IReadOnlyList<StockEntryRow> stockEntries);
}

public sealed class AdminReportBuilder
    : IAdminReportBuilder
{
    public AdminReportResponse Build(
        ReportsRequest request,
        IReadOnlyList<ReportProductData> products,
        IReadOnlyList<ReportOrderData> orders,
        IReadOnlyList<ReportOrderItemData> orderItems,
        IReadOnlyList<StockEntryRow> stockEntries)
    {
        List<SalesRow> sales =
            BuildSales(orderItems);

        List<InventoryRow> inventory =
            BuildInventory(
                products,
                sales,
                stockEntries);

        List<string> categories =
            products
                .Select(product =>
                    product.CategoryName)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();

        ReportSummary summary =
            BuildSummary(
                request.LowStockThreshold,
                products,
                orders,
                sales,
                stockEntries);

        return new AdminReportResponse(
            request.From,
            request.To,
            request.LowStockThreshold,
            summary,
            categories,
            inventory,
            sales,
            stockEntries);
    }

    private static List<SalesRow> BuildSales(
        IReadOnlyList<ReportOrderItemData> orderItems) =>
        orderItems
            .GroupBy(item => new
            {
                item.ProductId,
                item.Title
            })
            .Select(group => new SalesRow(
                group.Key.ProductId,
                group.Key.Title,
                group.Sum(item => item.Quantity),
                group.Sum(item => item.TotalPrice),
                group
                    .Select(item => item.OrderId)
                    .Distinct()
                    .Count()))
            .OrderByDescending(row =>
                row.SoldQuantity)
            .ThenBy(row => row.ProductName)
            .ToList();

    private static List<InventoryRow> BuildInventory(
        IReadOnlyList<ReportProductData> products,
        IReadOnlyList<SalesRow> sales,
        IReadOnlyList<StockEntryRow> stockEntries)
    {
        Dictionary<Guid, int> receivedByProduct =
            stockEntries
                .GroupBy(entry => entry.ProductId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(
                        entry => entry.Quantity));

        Dictionary<Guid, int> soldByProduct =
            sales
                .GroupBy(row => row.ProductId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(
                        row => row.SoldQuantity));

        return products
            .Select(product =>
            {
                receivedByProduct.TryGetValue(
                    product.Id,
                    out int received);

                soldByProduct.TryGetValue(
                    product.Id,
                    out int sold);

                return new InventoryRow(
                    product.Id,
                    product.Title,
                    product.CategoryName,
                    product.Quantity,
                    received,
                    sold,
                    received - sold);
            })
            .OrderBy(row =>
                row.CurrentQuantity)
            .ThenBy(row => row.ProductName)
            .ToList();
    }

    private static ReportSummary BuildSummary(
        int lowStockThreshold,
        IReadOnlyList<ReportProductData> products,
        IReadOnlyList<ReportOrderData> orders,
        IReadOnlyList<SalesRow> sales,
        IReadOnlyList<StockEntryRow> stockEntries)
    {
        ulong totalUnitsInStock =
            products.Aggregate(
                0UL,
                (sum, product) =>
                    sum + product.Quantity);

        int receivedUnits =
            stockEntries.Sum(
                entry => entry.Quantity);

        int soldUnits =
            sales.Sum(
                row => row.SoldQuantity);

        int outOfStockProducts =
            products.Count(
                product =>
                    product.Quantity == 0);

        int lowStockProducts =
            products.Count(
                product =>
                    product.Quantity > 0 &&
                    product.Quantity <=
                    (uint)lowStockThreshold);

        return new ReportSummary(
            products.Count,
            totalUnitsInStock,
            lowStockProducts,
            outOfStockProducts,
            receivedUnits,
            soldUnits,
            orders.Count,
            orders.Sum(
                order =>
                    order.OrderTotalPrice));
    }
}
