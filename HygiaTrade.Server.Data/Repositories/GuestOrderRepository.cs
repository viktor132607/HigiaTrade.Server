using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Data.Repositories;

public sealed class GuestOrderRepository(
    ApplicationDbContext context)
    : IGuestOrderRepository
{
    public async Task<Dictionary<Guid, Product>>
        GetAvailableProductsAsync(
            IReadOnlyCollection<Guid> productIds) =>
        await context.Products
            .Where(product =>
                productIds.Contains(product.Id) &&
                product.IsActive &&
                !product.IsDeleted)
            .ToDictionaryAsync(product => product.Id);

    public async Task<Guid> SaveAsync(
        Order order,
        IReadOnlyCollection<Product> updatedProducts)
    {
        context.Products.UpdateRange(updatedProducts);
        context.Orders.Add(order);

        await context.SaveChangesAsync();

        return order.Id;
    }
}
