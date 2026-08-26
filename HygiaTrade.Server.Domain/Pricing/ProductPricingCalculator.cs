using HygiaTrade.Core.Enums;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Domain.Pricing;

public readonly record struct ProductPriceBreakdown(
    decimal UnitPriceInclVat,
    decimal UnitPriceExclVat,
    decimal VatRate,
    PricingTier PricingTier);

public static class ProductPricingCalculator
{
    public static ProductPriceBreakdown Calculate(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);

        bool useWholesale = quantity > 0
            && product.WholesalePrice > 0m
            && product.WholesaleMinQuantity > 0
            && (uint)quantity >= product.WholesaleMinQuantity;

        decimal unitPriceInclVat;

        if (useWholesale)
        {
            unitPriceInclVat = product.WholesalePrice;

            if (product.DiscountPercentage > 0)
            {
                unitPriceInclVat *= 1m - product.DiscountPercentage / 100m;
            }
        }
        else
        {
            unitPriceInclVat = product.DiscountedPrice > 0m
                ? product.DiscountedPrice
                : product.RegularPrice;
        }

        unitPriceInclVat = RoundMoney(unitPriceInclVat);
        decimal unitPriceExclVat = GrossToNet(unitPriceInclVat, product.VatRate);

        return new ProductPriceBreakdown(
            unitPriceInclVat,
            unitPriceExclVat,
            product.VatRate,
            useWholesale ? PricingTier.Wholesale : PricingTier.Retail);
    }

    public static decimal GrossToNet(decimal grossAmount, decimal vatRate)
    {
        if (grossAmount <= 0m)
        {
            return 0m;
        }

        ValidateVatRate(vatRate);

        decimal divisor = 1m + vatRate / 100m;
        return RoundMoney(grossAmount / divisor);
    }

    public static decimal NetToGross(decimal netAmount, decimal vatRate)
    {
        if (netAmount <= 0m)
        {
            return 0m;
        }

        ValidateVatRate(vatRate);
        return RoundMoney(netAmount * (1m + vatRate / 100m));
    }

    public static decimal VatFromGross(decimal grossAmount, decimal vatRate)
    {
        return RoundMoney(grossAmount - GrossToNet(grossAmount, vatRate));
    }

    public static decimal RoundMoney(decimal amount)
    {
        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    public static void ValidateVatRate(decimal vatRate)
    {
        if (vatRate < 0m || vatRate > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(vatRate), "VAT rate must be between 0 and 100 percent.");
        }
    }
}
