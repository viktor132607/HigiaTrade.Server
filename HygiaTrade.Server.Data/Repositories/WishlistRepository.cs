using Microsoft.EntityFrameworkCore;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;

namespace HygiaTrade.Data.Repositories;

public class WishlistRepository(ApplicationDbContext context) : Repository<WishlistItem>(context), IWishlistRepository
{
    public async Task<bool> IsProductAlreadyInWishlist(Guid productId, Guid wishlistId)
    {
        return context.WishlistItems.Any(x => x.ProductId == productId && x.Id == wishlistId && x.IsDeleted == false);
    }
    
    public async Task<ICollection<WishlistItem>> GetAllByUserIdAsync(Guid userId)
    {
        return await context.WishlistItems
            .Include(w => w.Product)
            .ThenInclude(p => p.Category)
            .Where(w => w.UserId == userId && w.IsDeleted == false)
            .Include(w => w.Product)
            .ToListAsync();
    }
    
    public async Task<Guid?> GetWishlistItemIdAsync(Guid userId, Guid productId)
    {
        return await context.WishlistItems
            .Include(w => w.Product)
            .ThenInclude(p => p.Category)
            .Where(w => w.UserId == userId && w.ProductId == productId && w.IsDeleted == false)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync();
    }
}
