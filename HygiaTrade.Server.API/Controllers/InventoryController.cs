using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class InventoryController(ApplicationDbContext db) : ControllerBase
{
    public sealed record AddStockRequest(int Quantity, string InvoiceNumber);

    public sealed record StockEntryResponse(
        Guid Id,
        int Quantity,
        string InvoiceNumber,
        DateTime CreatedOn);

    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> GetAsync(Guid productId)
    {
        var product = await db.Products
            .AsNoTracking()
            .Where(product => product.Id == productId && !product.IsDeleted)
            .Select(product => new { product.Id, product.Quantity })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            return NotFound(new { message = "Product not found." });
        }

        List<StockEntryResponse> entries = [];
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT \"Id\", \"Quantity\", \"InvoiceNumber\", \"CreatedOn\" " +
                "FROM \"StockEntries\" WHERE \"ProductId\" = @productId " +
                "ORDER BY \"CreatedOn\" DESC";

            var productParameter = command.CreateParameter();
            productParameter.ParameterName = "@productId";
            productParameter.Value = productId;
            command.Parameters.Add(productParameter);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                entries.Add(new StockEntryResponse(
                    reader.GetGuid(0),
                    reader.GetInt32(1),
                    reader.GetString(2),
                    reader.GetDateTime(3)));
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return Ok(new
        {
            currentQuantity = product.Quantity,
            entries
        });
    }

    [HttpPost("{productId:guid}/add")]
    public async Task<IActionResult> AddAsync(Guid productId, [FromBody] AddStockRequest request)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest(new { message = "Quantity must be greater than zero." });
        }

        string invoiceNumber = request.InvoiceNumber?.Trim() ?? string.Empty;
        if (invoiceNumber.Length == 0)
        {
            return BadRequest(new { message = "Invoice number is required." });
        }

        if (invoiceNumber.Length > 100)
        {
            return BadRequest(new { message = "Invoice number cannot exceed 100 characters." });
        }

        var product = await db.Products
            .FirstOrDefaultAsync(item => item.Id == productId && !item.IsDeleted);

        if (product is null)
        {
            return NotFound(new { message = "Product not found." });
        }

        if ((ulong)product.Quantity + (ulong)request.Quantity > uint.MaxValue)
        {
            return BadRequest(new { message = "The resulting quantity is too large." });
        }

        Guid entryId = Guid.NewGuid();
        DateTime createdOn = DateTime.UtcNow;

        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            product.Quantity += (uint)request.Quantity;
            product.ModifiedOn = createdOn;
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ""StockEntries"" (""Id"", ""ProductId"", ""Quantity"", ""InvoiceNumber"", ""CreatedOn"")
                VALUES ({entryId}, {productId}, {request.Quantity}, {invoiceNumber}, {createdOn})");

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return Ok(new
        {
            currentQuantity = product.Quantity,
            entry = new StockEntryResponse(entryId, request.Quantity, invoiceNumber, createdOn)
        });
    }
}
