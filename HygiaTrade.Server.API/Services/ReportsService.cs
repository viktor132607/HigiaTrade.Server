using System.Data;
using HygiaTrade.API.Models;
using HygiaTrade.Core.Enums;
using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

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
    ApplicationDbContext db,
    ILogger<ReportsService> logger) : IReportsService
{
    public async Task<AdminReportResponse> GetAsync(
        DateOnly? from,
        DateOnly? to,
        int lowStockThreshold,
        CancellationToken cancellationToken = default)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly fromDateOnly = from ?? today.AddDays(-30);
        DateOnly toDateOnly = to ?? today;

        if (fromDateOnly > toDateOnly)
        {
            throw new ReportsServiceException(
                StatusCodes.Status400BadRequest,
                "Началната дата не може да е след крайната дата.");
        }

        lowStockThreshold = Math.Clamp(lowStockThreshold, 0, 1_000_000);

        DateTime fromUtc = DateTime.SpecifyKind(
            fromDateOnly.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

        DateTime toExclusiveUtc = DateTime.SpecifyKind(
            toDateOnly.AddDays(1).ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc);

        var products = await db.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted)
            .Select(product => new
            {
                product.Id,
                product.Title,
                product.Quantity,
                CategoryName = product.Category != null
                    ? product.Category.Name
                    : "Без категория"
            })
            .OrderBy(product => product.Title)
            .ToListAsync(cancellationToken);

        var orders = await db.Orders
            .AsNoTracking()
            .Where(order =>
                !order.IsDeleted &&
                order.Status != OrderStatus.Cancelled &&
                order.CreatedOn >= fromUtc &&
                order.CreatedOn < toExclusiveUtc)
            .Select(order => new
            {
                order.Id,
                order.OrderTotalPrice
            })
            .ToListAsync(cancellationToken);

        Guid[] validOrderIds = orders
            .Select(order => order.Id)
            .ToArray();

        var orderItems = await db.OrderItems
            .AsNoTracking()
            .Where(item =>
                !item.IsDeleted &&
                validOrderIds.Contains(item.OrderId))
            .Select(item => new
            {
                item.ProductId,
                item.Title,
                item.Quantity,
                item.TotalPrice,
                item.OrderId
            })
            .ToListAsync(cancellationToken);

        List<SalesRow> sales = orderItems
            .GroupBy(item => new { item.ProductId, item.Title })
            .Select(group => new SalesRow(
                group.Key.ProductId,
                group.Key.Title,
                group.Sum(item => item.Quantity),
                group.Sum(item => item.TotalPrice),
                group.Select(item => item.OrderId).Distinct().Count()))
            .OrderByDescending(row => row.SoldQuantity)
            .ThenBy(row => row.ProductName)
            .ToList();

        List<StockEntryRow> stockEntries =
            await ReadStockEntriesSafeAsync(
                fromUtc,
                toExclusiveUtc,
                cancellationToken);

        Dictionary<Guid, int> receivedByProduct = stockEntries
            .GroupBy(entry => entry.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(entry => entry.Quantity));

        Dictionary<Guid, int> soldByProduct = sales
            .GroupBy(row => row.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(row => row.SoldQuantity));

        List<InventoryRow> inventory = products
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
            .OrderBy(row => row.CurrentQuantity)
            .ThenBy(row => row.ProductName)
            .ToList();

        ulong totalUnitsInStock = products.Aggregate(
            0UL,
            (sum, product) => sum + product.Quantity);

        int receivedUnits = stockEntries.Sum(entry => entry.Quantity);
        int soldUnits = sales.Sum(row => row.SoldQuantity);
        int totalOrders = orders.Count;
        decimal revenue = orders.Sum(order => order.OrderTotalPrice);
        int outOfStockProducts =
            products.Count(product => product.Quantity == 0);

        int lowStockProducts = products.Count(product =>
            product.Quantity > 0 &&
            product.Quantity <= (uint)lowStockThreshold);

        List<string> categories = products
            .Select(product => product.CategoryName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        return new AdminReportResponse(
            fromDateOnly,
            toDateOnly,
            lowStockThreshold,
            new ReportSummary(
                products.Count,
                totalUnitsInStock,
                lowStockProducts,
                outOfStockProducts,
                receivedUnits,
                soldUnits,
                totalOrders,
                revenue),
            categories,
            inventory,
            sales,
            stockEntries);
    }

    private async Task<List<StockEntryRow>> ReadStockEntriesSafeAsync(
        DateTime fromUtc,
        DateTime toExclusiveUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ReadStockEntriesAsync(
                fromUtc,
                toExclusiveUtc,
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "StockEntries could not be read. The rest of the report will still be returned.");

            return [];
        }
    }

    private async Task<List<StockEntryRow>> ReadStockEntriesAsync(
        DateTime fromUtc,
        DateTime toExclusiveUtc,
        CancellationToken cancellationToken)
    {
        List<StockEntryRow> result = [];
        var connection = db.Database.GetDbConnection();
        bool shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();

            command.CommandText = """
                SELECT
                    s."Id",
                    s."ProductId",
                    p."Title",
                    COALESCE(c."Name", 'Без категория'),
                    s."Quantity",
                    s."InvoiceNumber",
                    s."CreatedOn"
                FROM "StockEntries" s
                INNER JOIN "Products" p ON p."Id" = s."ProductId"
                LEFT JOIN "Categories" c ON c."Id" = p."CategoryId"
                WHERE p."IsDeleted" = FALSE
                  AND s."CreatedOn" >= @fromUtc
                  AND s."CreatedOn" < @toExclusiveUtc
                ORDER BY s."CreatedOn" DESC
                """;

            var fromParameter = command.CreateParameter();
            fromParameter.ParameterName = "@fromUtc";
            fromParameter.Value = fromUtc;
            command.Parameters.Add(fromParameter);

            var toParameter = command.CreateParameter();
            toParameter.ParameterName = "@toExclusiveUtc";
            toParameter.Value = toExclusiveUtc;
            command.Parameters.Add(toParameter);

            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new StockEntryRow(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetInt32(4),
                    reader.GetString(5),
                    reader.GetDateTime(6)));
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return result;
    }
}
