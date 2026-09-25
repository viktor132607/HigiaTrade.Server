using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Interfaces;

public interface IGuestOrderRepository
{
    Task<Dictionary<Guid, Product>>
        GetAvailableProductsAsync(
            IReadOnlyCollection<Guid> productIds);

    Task<Guid> SaveAsync(
        Order order,
        IReadOnlyCollection<Product> updatedProducts);
}
