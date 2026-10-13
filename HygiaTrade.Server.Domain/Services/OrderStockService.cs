using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;

namespace HygiaTrade.Domain.Services;

public interface IOrderStockService
{
    Task EnsureAvailabilityAsync(Order order);

    Task DecreaseQuantitiesAsync(Order order);
}

public sealed class OrderStockService(
    IProductRepository productRepository)
    : IOrderStockService
{
    public async Task EnsureAvailabilityAsync(
        Order order)
    {
        foreach (OrderItem item in order.Items)
        {
            Product? product =
                await productRepository.GetByIdAsync(
                    item.ProductId);

            if (product is null || !product.IsActive || product.IsDeleted ||
                ProductPackagingPolicy.Available(product,
                    ProductPackagingPolicy.Resolve(product, item.PackagingOptionId)) < item.Quantity)
            {
                throw new AppException(
                        $"Product '{item.Title}' is out of stock or has insufficient quantity.")
                    .SetStatusCode(409);
            }
        }
    }

    public async Task DecreaseQuantitiesAsync(
        Order order)
    {
        foreach (OrderItem item in order.Items)
        {
            Product? product =
                await productRepository.GetByIdAsync(
                    item.ProductId);

            if (product is null)
            {
                throw new AppException("Product not found")
                    .SetStatusCode(404);
            }

            var packaging = ProductPackagingPolicy.Resolve(product, item.PackagingOptionId);
            if (ProductPackagingPolicy.Available(product, packaging) < item.Quantity)
            {
                throw new AppException(
                        $"Product '{item.Title}' is out of stock or has insufficient quantity.")
                    .SetStatusCode(409);
            }

            ProductPackagingPolicy.Take(product, packaging, item.Quantity);

            await productRepository.UpdateAsync(product);
        }
    }
}
