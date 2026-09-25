using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Pricing;

namespace HygiaTrade.Domain.Services;

public interface IProductPricingPolicy
{
    void Validate(
        decimal regularPrice,
        byte discountPercentage,
        decimal discountedPrice,
        decimal wholesalePrice,
        uint wholesaleMinQuantity,
        decimal vatRate);

    void ApplyRetailPricing(
        Product product,
        decimal regularPrice,
        byte discountPercentage,
        decimal discountedPrice);

    ProductPriceQuoteResponse CreateQuote(
        Product product,
        int quantity);
}

public sealed class ProductPricingPolicy
    : IProductPricingPolicy
{
    public void Validate(
        decimal regularPrice,
        byte discountPercentage,
        decimal discountedPrice,
        decimal wholesalePrice,
        uint wholesaleMinQuantity,
        decimal vatRate)
    {
        if (regularPrice < 0m)
        {
            throw BadRequest(
                "Retail price cannot be negative.");
        }

        if (discountPercentage > 100)
        {
            throw BadRequest(
                "Discount percentage must be between 0 and 100.");
        }

        if (discountedPrice < 0m)
        {
            throw BadRequest(
                "Discounted price cannot be negative.");
        }

        if (discountedPrice > 0m &&
            regularPrice <= 0m)
        {
            throw BadRequest(
                "A positive retail price is required when a discounted price is set.");
        }

        if (discountedPrice > regularPrice &&
            regularPrice > 0m)
        {
            throw BadRequest(
                "Discounted price cannot exceed the retail price.");
        }

        if (wholesalePrice < 0m)
        {
            throw BadRequest(
                "Wholesale price cannot be negative.");
        }

        bool wholesalePriceConfigured =
            wholesalePrice > 0m;

        bool wholesaleQuantityConfigured =
            wholesaleMinQuantity > 0;

        if (wholesalePriceConfigured !=
            wholesaleQuantityConfigured)
        {
            throw BadRequest(
                "Wholesale price and minimum quantity must either both be configured or both be zero.");
        }

        try
        {
            ProductPricingCalculator.ValidateVatRate(
                vatRate);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw BadRequest(
                "VAT rate must be between 0 and 100 percent.");
        }
    }

    public void ApplyRetailPricing(
        Product product,
        decimal regularPrice,
        byte discountPercentage,
        decimal discountedPrice)
    {
        product.RegularPrice =
            ProductPricingCalculator.RoundMoney(
                regularPrice);

        if (discountedPrice > 0m)
        {
            product.DiscountedPrice =
                ProductPricingCalculator.RoundMoney(
                    discountedPrice);

            product.DiscountPercentage =
                product.RegularPrice == 0m
                    ? (byte)0
                    : (byte)Math.Clamp(
                        (int)Math.Round(
                            (1m -
                             product.DiscountedPrice /
                             product.RegularPrice) *
                            100m,
                            0,
                            MidpointRounding.AwayFromZero),
                        0,
                        100);

            return;
        }

        product.DiscountPercentage =
            discountPercentage;

        product.DiscountedPrice =
            discountPercentage == 0
                ? 0m
                : ProductPricingCalculator.RoundMoney(
                    product.RegularPrice *
                    (1m -
                     discountPercentage / 100m));
    }

    public ProductPriceQuoteResponse CreateQuote(
        Product product,
        int quantity)
    {
        ProductPriceBreakdown pricing =
            ProductPricingCalculator.Calculate(
                product,
                quantity);

        decimal totalInclVat =
            ProductPricingCalculator.RoundMoney(
                pricing.UnitPriceInclVat * quantity);

        decimal totalExclVat =
            ProductPricingCalculator.GrossToNet(
                totalInclVat,
                pricing.VatRate);

        decimal vatAmount =
            ProductPricingCalculator.RoundMoney(
                totalInclVat - totalExclVat);

        return new ProductPriceQuoteResponse
        {
            ProductId = product.Id,
            Quantity = quantity,
            PricingTier =
                pricing.PricingTier.ToString(),
            WholesaleMinQuantity =
                product.WholesaleMinQuantity,
            VatRate = pricing.VatRate,
            UnitPriceExclVat =
                pricing.UnitPriceExclVat,
            UnitPriceInclVat =
                pricing.UnitPriceInclVat,
            TotalExclVat = totalExclVat,
            VatAmount = vatAmount,
            TotalInclVat = totalInclVat,
        };
    }

    private static AppException BadRequest(
        string message) =>
        new AppException(message).SetStatusCode(400);
}
