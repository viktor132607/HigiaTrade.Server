using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public sealed record AdminCommandResult(int AffectedProducts, decimal? Price, uint? Quantity, int AffectedPackagingOptions);

public interface IAdminCommandService
{
    Task<AdminCommandResult> ExecuteAsync(string command, CancellationToken cancellationToken);
}

public sealed class AdminCommandService(ApplicationDbContext db) : IAdminCommandService
{
    public async Task<AdminCommandResult> ExecuteAsync(string command, CancellationToken cancellationToken)
    {
        var parsed = AdminCommandParser.Parse(command);
        var setPrice = parsed.Price.HasValue;
        var setQuantity = parsed.Quantity.HasValue;
        var price = parsed.Price.GetValueOrDefault();
        var quantity = parsed.Quantity.GetValueOrDefault();
        var now = DateTime.UtcNow;

        // All product and packaging stock updates succeed or roll back together.
        // A price-only command leaves packaging-specific prices and stock untouched.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var affected = await db.Products.Where(p => !p.IsDeleted).ExecuteUpdateAsync(setters => setters
            .SetProperty(p => p.RegularPrice, p => setPrice ? price : p.RegularPrice)
            .SetProperty(p => p.DiscountedPrice, p => setPrice ? price : p.DiscountedPrice)
            .SetProperty(p => p.DiscountPercentage, p => setPrice ? (byte)0 : p.DiscountPercentage)
            .SetProperty(p => p.WholesalePrice, p => setPrice && p.WholesalePrice > 0 ? price : p.WholesalePrice)
            .SetProperty(p => p.Quantity, p => setQuantity ? quantity : p.Quantity)
            .SetProperty(p => p.ModifiedOn, now), cancellationToken);

        var affectedPackaging = 0;
        if (setQuantity)
        {
            // Includes active/inactive variants, but never deleted variants or products.
            // Base product stock is the source of truth for default packaging.
            affectedPackaging = await AdminCommandStockTargets.Packaging(db)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(option => option.StockQuantity, quantity)
                    .SetProperty(option => option.ModifiedOn, now), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new AdminCommandResult(affected, parsed.Price, parsed.Quantity, affectedPackaging);
    }
}


// The same eligible set is used for updates and coverage tests.
public static class AdminCommandStockTargets
{
    public static IQueryable<ProductPackagingOption> Packaging(ApplicationDbContext db) =>
        db.ProductPackagingOptions.Where(option => !option.IsDeleted &&
            db.Products.Any(product => product.Id == option.ProductId && !product.IsDeleted));
}
