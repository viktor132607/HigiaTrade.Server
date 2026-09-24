namespace HygiaTrade.API.Models;

public sealed record InventoryRow(
    Guid ProductId,
    string ProductName,
    string CategoryName,
    uint CurrentQuantity,
    int ReceivedQuantity,
    int SoldQuantity,
    int NetMovement);

public sealed record SalesRow(
    Guid ProductId,
    string ProductName,
    int SoldQuantity,
    decimal Revenue,
    int OrderCount);

public sealed record StockEntryRow(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string CategoryName,
    int Quantity,
    string InvoiceNumber,
    DateTime CreatedOn);

public sealed record ReportSummary(
    int TotalProducts,
    ulong TotalUnitsInStock,
    int LowStockProducts,
    int OutOfStockProducts,
    int ReceivedUnits,
    int SoldUnits,
    int TotalOrders,
    decimal Revenue);

public sealed record AdminReportResponse(
    DateOnly From,
    DateOnly To,
    int LowStockThreshold,
    ReportSummary Summary,
    IReadOnlyList<string> Categories,
    IReadOnlyList<InventoryRow> Inventory,
    IReadOnlyList<SalesRow> Sales,
    IReadOnlyList<StockEntryRow> StockEntries);
