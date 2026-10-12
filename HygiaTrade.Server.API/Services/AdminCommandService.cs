using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public sealed record AdminCommandResult(int AffectedProducts, decimal? Price, uint? Quantity);

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

        // One atomic database statement, including inactive products and every category.
        // A uniform price also removes discounts and sets enabled wholesale prices to the same amount.
        var affected = await db.Products.Where(p => !p.IsDeleted).ExecuteUpdateAsync(setters => setters
            .SetProperty(p => p.RegularPrice, p => setPrice ? price : p.RegularPrice)
            .SetProperty(p => p.DiscountedPrice, p => setPrice ? price : p.DiscountedPrice)
            .SetProperty(p => p.DiscountPercentage, p => setPrice ? (byte)0 : p.DiscountPercentage)
            .SetProperty(p => p.WholesalePrice, p => setPrice && p.WholesalePrice > 0 ? price : p.WholesalePrice)
            .SetProperty(p => p.Quantity, p => setQuantity ? quantity : p.Quantity)
            .SetProperty(p => p.ModifiedOn, now), cancellationToken);

        return new AdminCommandResult(affected, parsed.Price, parsed.Quantity);
    }
}
