using HygiaTrade.Common.Responses.Image;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Pricing;

namespace HygiaTrade.Domain.Services;

public static class ProductMapping
{
    public static ProductResponse ToProductResponse(
        Product product)
    {
        ProductPriceSnapshot pricing =
            BuildPricingSnapshot(product);

        return new ProductResponse
        {
            Id = product.Id,
            Title = product.Title,
            Brand = product.Brand,
            Description = product.Description,
            MainImageUrl = product.MainImageUrl,
            IsActive = product.IsActive,
            RegularPrice = product.RegularPrice,
            DiscountPercentage =
                product.DiscountPercentage,
            DiscountedPrice =
                product.DiscountedPrice,
            RetailPriceInclVat =
                product.RegularPrice,
            RetailPriceExclVat =
                pricing.RetailExclVat,
            DiscountedPriceInclVat =
                product.DiscountedPrice,
            DiscountedPriceExclVat =
                pricing.DiscountedExclVat,
            WholesalePriceInclVat =
                product.WholesalePrice,
            WholesalePriceExclVat =
                pricing.WholesaleExclVat,
            WholesaleMinQuantity =
                product.WholesaleMinQuantity,
            VatRate = product.VatRate,
            WholesaleEnabled =
                pricing.WholesaleEnabled,
            Rating = product.Rating,
            Quantity = product.Quantity,
            CategoryId = product.CategoryId,
            CategoryName =
                product.Category?.Name ??
                string.Empty,
            SecondaryImages =
                MapImages(product)
        };
    }

    public static ProductsResponse ToProductsResponse(
        Product product)
    {
        ProductPriceSnapshot pricing =
            BuildPricingSnapshot(product);

        return new ProductsResponse
        {
            Id = product.Id,
            Title = product.Title,
            Brand = product.Brand,
            Description = product.Description,
            MainImageUrl = product.MainImageUrl,
            IsActive = product.IsActive,
            RegularPrice = product.RegularPrice,
            DiscountPercentage =
                product.DiscountPercentage,
            DiscountedPrice =
                product.DiscountedPrice,
            RetailPriceInclVat =
                product.RegularPrice,
            RetailPriceExclVat =
                pricing.RetailExclVat,
            DiscountedPriceInclVat =
                product.DiscountedPrice,
            DiscountedPriceExclVat =
                pricing.DiscountedExclVat,
            WholesalePriceInclVat =
                product.WholesalePrice,
            WholesalePriceExclVat =
                pricing.WholesaleExclVat,
            WholesaleMinQuantity =
                product.WholesaleMinQuantity,
            VatRate = product.VatRate,
            WholesaleEnabled =
                pricing.WholesaleEnabled,
            Quantity = product.Quantity,
            Rating = product.Rating,
            CategoryId = product.CategoryId,
            CategoryName =
                product.Category?.Name ??
                string.Empty,
            SecondaryImages =
                MapImages(product)
        };
    }

    private static ProductPriceSnapshot
        BuildPricingSnapshot(Product product) =>
        new(
            ProductPricingCalculator.GrossToNet(
                product.RegularPrice,
                product.VatRate),
            ProductPricingCalculator.GrossToNet(
                product.DiscountedPrice,
                product.VatRate),
            ProductPricingCalculator.GrossToNet(
                product.WholesalePrice,
                product.VatRate),
            product.WholesalePrice > 0m &&
            product.WholesaleMinQuantity > 0);

    private static List<ImageResponse> MapImages(
        Product product) =>
        product.SecondaryImages
            .Select(image =>
                new ImageResponse
                {
                    Id = image.Id,
                    Uri = image.Uri
                })
            .ToList();

    private readonly record struct ProductPriceSnapshot(
        decimal RetailExclVat,
        decimal DiscountedExclVat,
        decimal WholesaleExclVat,
        bool WholesaleEnabled);
}
