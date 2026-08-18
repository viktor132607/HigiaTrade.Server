using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Interfaces;

public interface IWishlistRepository : IRepository<WishlistItem>
{
    Task<bool> IsProductAlreadyInWishlist(Guid productId, Guid wishlistId);
    Task<ICollection<WishlistItem>> GetAllByUserIdAsync(Guid userId);
    Task<Guid?> GetWishlistItemIdAsync(Guid userId, Guid productId);
}
