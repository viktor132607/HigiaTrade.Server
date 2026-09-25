using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;

namespace HygiaTrade.Domain.Services;

public interface IProductReadService
{
    Task<IEnumerable<ProductResponse>?> GetAsync();

    Task<IEnumerable<ProductResponse>?> GetBestSellersAsync(
        int numOfBestSellers);

    Task<ProductResponse?> GetByIdAsync(Guid id);

    Task<ProductPriceQuoteResponse> GetPriceQuoteAsync(
        Guid id,
        int quantity);
}

public sealed class ProductReadService(
    IProductRepository productRepository,
    IProductPricingPolicy pricingPolicy)
    : IProductReadService
{
    public async Task<IEnumerable<ProductResponse>?> GetAsync()
    {
        IEnumerable<Product> products =
            await productRepository.GetAllAsync();

        return products.Select(ProductMapping.ToProductResponse);
    }

    public async Task<IEnumerable<ProductResponse>?>
        GetBestSellersAsync(int numOfBestSellers)
    {
        IEnumerable<Product> products =
            await productRepository.GetBestSellersAsync(
                numOfBestSellers);

        return products.Select(ProductMapping.ToProductResponse);
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id)
    {
        Product? product =
            await productRepository.GetByIdAsync(id);

        if (product is null)
        {
            throw new AppException("Product not found.")
                .SetStatusCode(404);
        }

        return ProductMapping.ToProductResponse(product);
    }

    public async Task<ProductPriceQuoteResponse>
        GetPriceQuoteAsync(Guid id, int quantity)
    {
        if (quantity <= 0)
        {
            throw new AppException(
                    "Quantity must be greater than zero.")
                .SetStatusCode(400);
        }

        Product? product =
            await productRepository.GetByIdAsync(id);

        if (product is null)
        {
            throw new AppException("Product not found.")
                .SetStatusCode(404);
        }

        return pricingPolicy.CreateQuote(product, quantity);
    }
}
