using HygiaTrade.API.Controllers;
using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public sealed record InventoryStockAdjustment(
    uint CurrentQuantity,
    StockEntryResponse Entry);

public interface IInventoryStockAdder
{
    Task<InventoryStockAdjustment> AddAsync(
        Guid productId,
        InventoryStockRequest request);
}

public sealed class InventoryStockAdder(
    ApplicationDbContext db,
    IInventoryRequestValidator validator,
    IInventoryClock clock)
    : IInventoryStockAdder
{
    public async Task<InventoryStockAdjustment> AddAsync(
        Guid productId,
        InventoryStockRequest request)
    {
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

        validator.EnsureCanAdd(
            product.Quantity,
            request.Quantity);

        Guid entryId = Guid.NewGuid();
        DateTime createdOn = clock.UtcNow;

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
                    VALUES ({entryId}, {productId}, {request.Quantity}, {request.InvoiceNumber}, {createdOn})");

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
                request.InvoiceNumber,
                createdOn);

        return new InventoryStockAdjustment(
            product.Quantity,
            entry);
    }
}
