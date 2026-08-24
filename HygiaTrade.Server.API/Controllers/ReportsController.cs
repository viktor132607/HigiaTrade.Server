using System.Data;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class ReportsController(ApplicationDbContext db) : ControllerBase
{
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

    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int lowStockThreshold = 10)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDateOnly = from ?? today.AddDays(-30);
        var toDateOnly = to ?? today;

        if (fromDateOnly > toDateOnly)
        {
            return BadRequest(new { message = "Началната дата не може да е след крайната дата." });
        }

        lowStockThreshold = Math.Clamp(lowStockThreshold, 0, 1_000_000);

        var fromUtc = DateTime.SpecifyKind(fromDateOnly.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var toUtc = DateTime.SpecifyKind(toDateOnly.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

        var products = await db.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted)
            .Select(product => new
            {
                product.Id,
                product.Title,
                product.Quantity,
                CategoryName = product.Category != null ? product.Category.Name : "Без категория"
            })
            .OrderBy(product => product.Title)
            .ToListAsync();

        var validOrders = db.Orders
            .AsNoTracking()
            .Where(order =>
                !order.IsDeleted &&
                order.Status != OrderStatus.Cancelled &&
                order.CreatedOn >= fromUtc &&
                order.CreatedOn <= toUtc);

        var totalOrders = await validOrders.CountAsync();
        var revenue = await validOrders
            .Select(order => (decimal?)order.OrderTotalPrice)
            .SumAsync() ?? 0m;

        var sales = await db.OrderItems
            .AsNoTracking()
            .Where(item =>
                !item.IsDeleted &&
                !item.Order.IsDeleted &&
                item.Order.Status != OrderStatus.Cancelled &&
                item.Order.CreatedOn >= fromUtc &&
                item.Order.CreatedOn <= toUtc)
            .GroupBy(item => new { item.ProductId, item.Title })
            .Select(group => new SalesRow(
                group.Key.ProductId,
                group.Key.Title,
                group.Sum(item => item.Quantity),
                group.Sum(item => item.TotalPrice),
                group.Select(item => item.OrderId).Distinct().Count()))
            .OrderByDescending(row => row.SoldQuantity)
            .ThenBy(row => row.ProductName)
            .ToListAsync();

        var stockEntries = await ReadStockEntriesAsync(fromUtc, toUtc);

        var receivedByProduct = stockEntries
            .GroupBy(entry => entry.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(entry => entry.Quantity));
        var soldByProduct = sales
            .GroupBy(row => row.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.SoldQuantity));

        var inventory = products
            .Select(product =>
            {
                receivedByProduct.TryGetValue(product.Id, out var received);
                soldByProduct.TryGetValue(product.Id, out var sold);

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

        var totalUnitsInStock = products.Aggregate(0UL, (sum, product) => sum + product.Quantity);
        var receivedUnits = stockEntries.Sum(entry => entry.Quantity);
        var soldUnits = sales.Sum(row => row.SoldQuantity);
        var outOfStockProducts = products.Count(product => product.Quantity == 0);
        var lowStockProducts = products.Count(product =>
            product.Quantity > 0 && product.Quantity <= (uint)lowStockThreshold);

        var categories = products
            .Select(product => product.CategoryName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToList();

        return Ok(new
        {
            from = fromDateOnly,
            to = toDateOnly,
            lowStockThreshold,
            summary = new
            {
                totalProducts = products.Count,
                totalUnitsInStock,
                lowStockProducts,
                outOfStockProducts,
                receivedUnits,
                soldUnits,
                totalOrders,
                revenue
            },
            categories,
            inventory,
            sales,
            stockEntries
        });
    }

    private async Task<List<StockEntryRow>> ReadStockEntriesAsync(DateTime fromUtc, DateTime toUtc)
    {
        var result = new List<StockEntryRow>();
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
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
                  AND s."CreatedOn" <= @toUtc
                ORDER BY s."CreatedOn" DESC
                """;

            var fromParameter = command.CreateParameter();
            fromParameter.ParameterName = "@fromUtc";
            fromParameter.Value = fromUtc;
            command.Parameters.Add(fromParameter);

            var toParameter = command.CreateParameter();
            toParameter.ParameterName = "@toUtc";
            toParameter.Value = toUtc;
            command.Parameters.Add(toParameter);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
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
