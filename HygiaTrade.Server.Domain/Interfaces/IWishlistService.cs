using HygiaTrade.Common.Responses.Wishlist;

namespace HygiaTrade.Domain.Interfaces;

public interface IWishlistService
{
    Task<WishlistResponse> GetByJWT();
    Task<bool> AddProductToWishlistAsync(Guid productId);
    Task<bool> RemoveProductFromWishlistAsync(Guid productId);
}
