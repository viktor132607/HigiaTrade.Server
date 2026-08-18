using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Common.Responses.Wishlist;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public class WishlistService(IAuthService authService, IWishlistRepository wishlistRepository, IUserRepository userRepository, IProductRepository productRepository) : IWishlistService
{
    public async Task<WishlistResponse> GetByJWT()
    {
        Guid userId = Guid.Parse(await authService.GetCurrentUserId());

        ICollection<WishlistItem> wishlistItems = await wishlistRepository.GetAllByUserIdAsync(userId);
        if (wishlistItems == null)
        {
            throw new AppException("No Wishlist Items Found").SetStatusCode(409);
        }
        List<ProductsResponse> productResponses = wishlistItems.Select(wi => new ProductsResponse
        {
            Id = wi.Product.Id,
            Title = wi.Product.Title,
            Description = wi.Product.Description,
            MainImageUrl = wi.Product.MainImageUrl,
            RegularPrice = wi.Product.RegularPrice,
            DiscountPercentage = wi.Product.DiscountPercentage,
            DiscountedPrice = wi.Product.DiscountedPrice,
            Rating = wi.Product.Rating,
            Quantity = wi.Product.Quantity,
            CategoryId = wi.Product.CategoryId,
            CategoryName = wi.Product.Category.Name 
        }).ToList();

        return new()
        {
            Products = productResponses
        };
    }

    public async Task<bool> AddProductToWishlistAsync(Guid productId)
    {
        Guid userId = Guid.Parse(await authService.GetCurrentUserId());

        if (await wishlistRepository.IsProductAlreadyInWishlist(productId, userId))
        {
            throw new AppException("Product is already in the wishlist.").SetStatusCode(409);
        }
        
        WishlistItem wishlistItem = new()
        {
            UserId = userId,
            ProductId = productId
        };

        await wishlistRepository.AddAsync(wishlistItem);

        return true;

    }

    public async Task<bool> RemoveProductFromWishlistAsync(Guid productId)
    {
        Guid userId = Guid.Parse(await authService.GetCurrentUserId());
        
        if (!await wishlistRepository.IsProductAlreadyInWishlist(productId, userId))
        {
            throw new AppException("Product is not in the wishlist.").SetStatusCode(409);
        }
        
        Guid? wishlistItemId = await wishlistRepository.GetWishlistItemIdAsync(userId, productId);

        if (wishlistItemId == null)
        {
            throw new AppException("Wishlist item not found.").SetStatusCode(404);
        }

        await wishlistRepository.DeleteAsync(wishlistItemId.Value);

        return true;
    }
}
