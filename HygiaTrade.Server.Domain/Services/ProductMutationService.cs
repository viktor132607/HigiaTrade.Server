using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Pricing;

namespace HygiaTrade.Domain.Services;

public interface IProductMutationService
{
    Task<ProductResponse?> CreateAsync(
        CreateProductRequest request);

    Task<ProductResponse?> UpdateAsync(
        UpdateProductRequest request);

    Task<bool> DeleteAsync(Guid id);
}

public sealed class ProductMutationService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IProductImageService imageService,
    IProductPricingPolicy pricingPolicy)
    : IProductMutationService
{
    public async Task<ProductResponse?> CreateAsync(
        CreateProductRequest request)
    {
        Category? category =
            await categoryRepository.GetByIdAsync(
                request.CategoryId);

        if (category is null)
        {
            throw new AppException("Invalid category.")
                .SetStatusCode(400);
        }

        pricingPolicy.Validate(
            request.RegularPrice,
            request.DiscountPercentage,
            request.DiscountedPrice,
            request.WholesalePrice,
            request.WholesaleMinQuantity,
            request.VatRate);

        var product = new Product
        {
            Title = request.Title,
            Brand = NormalizeBrand(request.Brand),
            Description = request.Description,
            MainImageUrl = request.MainImageUrl,
            IsActive = request.IsActive,
            Rating = 0,
            Quantity = 0,
            CategoryId = request.CategoryId,
            WholesalePrice =
                ProductPricingCalculator.RoundMoney(
                    request.WholesalePrice),
            WholesaleMinQuantity =
                request.WholesaleMinQuantity,
            VatRate = request.VatRate,
        };

        pricingPolicy.ApplyRetailPricing(
            product,
            request.RegularPrice,
            request.DiscountPercentage,
            request.DiscountedPrice);

        product =
            await productRepository.AddAsync(product)
            ?? throw new InvalidOperationException(
                "Failed to persist product.");

        List<Image> images =
            await imageService.AddAsync(
                product,
                request.SecondaryImages);

        product.Category = category;
        product.SecondaryImages = images;

        return ProductMapping.ToProductResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(
        UpdateProductRequest request)
    {
        Product? existingProduct =
            await productRepository.GetByIdAsync(request.Id);

        if (existingProduct is null)
        {
            throw new AppException("Product not found.")
                .SetStatusCode(404);
        }

        Category? category =
            await categoryRepository.GetByIdAsync(
                request.CategoryId);

        if (category is null)
        {
            throw new AppException("Invalid category.")
                .SetStatusCode(400);
        }

        decimal wholesalePrice =
            request.WholesalePrice ??
            existingProduct.WholesalePrice;

        uint wholesaleMinQuantity =
            request.WholesaleMinQuantity ??
            existingProduct.WholesaleMinQuantity;

        decimal vatRate =
            request.VatRate ?? existingProduct.VatRate;

        pricingPolicy.Validate(
            request.RegularPrice,
            request.DiscountPercentage,
            request.DiscountedPrice,
            wholesalePrice,
            wholesaleMinQuantity,
            vatRate);

        existingProduct.Title = request.Title;
        existingProduct.Brand =
            NormalizeBrand(request.Brand);
        existingProduct.Description =
            request.Description;
        existingProduct.MainImageUrl =
            request.MainImageUrl;
        existingProduct.IsActive = request.IsActive;
        existingProduct.CategoryId = request.CategoryId;
        existingProduct.WholesalePrice =
            ProductPricingCalculator.RoundMoney(
                wholesalePrice);
        existingProduct.WholesaleMinQuantity =
            wholesaleMinQuantity;
        existingProduct.VatRate = vatRate;

        pricingPolicy.ApplyRetailPricing(
            existingProduct,
            request.RegularPrice,
            request.DiscountPercentage,
            request.DiscountedPrice);

        await imageService.ReplaceAsync(
            existingProduct,
            request.SecondaryImages);

        Product updatedProduct =
            await productRepository.UpdateAsync(
                existingProduct)
            ?? throw new InvalidOperationException(
                "Failed to persist product updates.");

        updatedProduct.Category = category;

        return ProductMapping.ToProductResponse(
            updatedProduct);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        Product? product =
            await productRepository.GetByIdAsync(id);

        if (product is null)
        {
            throw new AppException("Product not found.")
                .SetStatusCode(404);
        }

        await imageService.DeleteAllAsync(product);

        return await productRepository.DeleteAsync(id);
    }

    private static string? NormalizeBrand(string? brand)
    {
        string? normalized = brand?.Trim();

        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }
}
