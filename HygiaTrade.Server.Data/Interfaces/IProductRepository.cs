using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetBestSellersAsync(int numOfBestSellers);

    Task UpdateRatingAsync(Guid productId, double rating);
}
