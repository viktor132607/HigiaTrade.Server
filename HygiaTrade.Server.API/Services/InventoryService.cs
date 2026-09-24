using System.Data;
using HygiaTrade.API.Controllers;
using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public interface IInventoryService
{
    Task<InventoryResponse> GetAsync(Guid productId);

    Task<AddStockResponse> AddAsync(
        Guid productId,
        AddStockRequest request);
}

public sealed class InventoryServiceException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class InventoryService(
    ApplicationDbContext db) : IInventoryService
{
    public async Task<InventoryResponse> GetAsync(Guid productId)
    {
        var product = await db.Products
            .AsNoTracking()
            .Where(product =>
                product.Id == productId &&
                !product.IsDeleted)
            .Select(product => new
            {
                product.Id,
                product.Quantity
            })
            .FirstOrDefaultAsync();

        if (product is null)
        {
            throw new InventoryServiceException(
                StatusCodes.Status404NotFound,
                "Product not found.");
        }

        List<StockEntryResponse> entries = [];

        var connection =
            db.Database.GetDbConnection();

        bool shouldClose =
            connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            await using var command =
                connection.CreateCommand();

            command.CommandText =
                "SELECT \"Id\", \"Quantity\", \"InvoiceNumber\", \"CreatedOn\" " +
                "FROM \"StockEntries\" WHERE \"ProductId\" = @productId " +
                "ORDER BY \"CreatedOn\" DESC";

            var productParameter =
                command.CreateParameter();

            productParameter.ParameterName =
                "@productId";

            productParameter.Value =
                productId;

            command.Parameters.Add(
                productParameter);

            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                entries.Add(
                    new StockEntryResponse(
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

        return new InventoryResponse(
            product.Quantity,
            entries);
    }

    public async Task<AddStockResponse> AddAsync(
        Guid productId,
        AddStockRequest request)
    {
        if (request.Quantity <= 0)
        {
            throw new InventoryServiceException(
                StatusCodes.Status400BadRequest,
                "Quantity must be greater than zero.");
        }

        string invoiceNumber =
            request.InvoiceNumber?.Trim() ??
            string.Empty;

        if (invoiceNumber.Length == 0)
        {
            throw new InventoryServiceException(
                StatusCodes.Status400BadRequest,
                "Invoice number is required.");
        }

        if (invoiceNumber.Length > 100)
        {
            throw new InventoryServiceException(
                StatusCodes.Status400BadRequest,
                "Invoice number cannot exceed 100 characters.");
        }

        var product = await db.Products
            .FirstOrDefaultAsync(item =>
                item.Id == productId &&
                !item.IsDeleted);

        if (product is null)
        {
            throw new InventoryServiceException(
                StatusCodes.Status404NotFound,
                "Product not found.");
        }

        if ((ulong)product.Quantity +
            (ulong)request.Quantity >
            uint.MaxValue)
        {
            throw new InventoryServiceException(
                StatusCodes.Status400BadRequest,
                "The resulting quantity is too large.");
        }

        Guid entryId = Guid.NewGuid();
        DateTime createdOn = DateTime.UtcNow;

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        try
        {
            product.Quantity +=
                (uint)request.Quantity;

            product.ModifiedOn =
                createdOn;

            await db.SaveChangesAsync();

            await db.Database
                .ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO ""StockEntries"" (""Id"", ""ProductId"", ""Quantity"", ""InvoiceNumber"", ""CreatedOn"")
                    VALUES ({entryId}, {productId}, {request.Quantity}, {invoiceNumber}, {createdOn})");

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        StockEntryResponse entry =
            new(
                entryId,
                request.Quantity,
                invoiceNumber,
                createdOn);

        return new AddStockResponse(
            product.Quantity,
            entry);
    }
}
