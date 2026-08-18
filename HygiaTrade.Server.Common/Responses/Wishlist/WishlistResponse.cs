using HygiaTrade.Common.Responses.Product;

namespace HygiaTrade.Common.Responses.Wishlist;

public class WishlistResponse
{
    public ICollection<ProductsResponse> Products { get; set; } = new List<ProductsResponse>();
}
