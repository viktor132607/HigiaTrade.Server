using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class ProductPricingPolicyTests
{
    private readonly ProductPricingPolicy policy = new();

    public static IEnumerable<object[]> InvalidPricingCases()
    {
        yield return [-1m, (byte)0, 0m, 0m, (uint)0, 20m, "Retail price cannot be negative."];
        yield return [10m, (byte)101, 0m, 0m, (uint)0, 20m, "Discount percentage must be between 0 and 100."];
        yield return [10m, (byte)0, -1m, 0m, (uint)0, 20m, "Discounted price cannot be negative."];
        yield return [0m, (byte)0, 1m, 0m, (uint)0, 20m, "A positive retail price is required when a discounted price is set."];
        yield return [10m, (byte)0, 11m, 0m, (uint)0, 20m, "Discounted price cannot exceed the retail price."];
        yield return [10m, (byte)0, 0m, -1m, (uint)0, 20m, "Wholesale price cannot be negative."];
        yield return [10m, (byte)0, 0m, 5m, (uint)0, 20m, "Wholesale price and minimum quantity must either both be configured or both be zero."];
        yield return [10m, (byte)0, 0m, 0m, (uint)5, 20m, "Wholesale price and minimum quantity must either both be configured or both be zero."];
        yield return [10m, (byte)0, 0m, 0m, (uint)0, -1m, "VAT rate must be between 0 and 100 percent."];
        yield return [10m, (byte)0, 0m, 0m, (uint)0, 101m, "VAT rate must be between 0 and 100 percent."];
    }

    [Theory]
    [MemberData(nameof(InvalidPricingCases))]
    public void Validate_RejectsInvalidPricing(
        decimal regular,
        byte discountPercent,
        decimal discounted,
        decimal wholesale,
        uint minQty,
        decimal vat,
        string expectedMessage)
    {
        AppException ex = Assert.Throws<AppException>(
            () => policy.Validate(
                regular,
                discountPercent,
                discounted,
                wholesale,
                minQty,
                vat));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(expectedMessage, ex.Message);
    }

    [Fact]
    public void Validate_AcceptsValidRetailAndWholesalePricing()
    {
        policy.Validate(10m, 10, 9m, 7m, 5, 20m);
    }

    [Fact]
    public void ApplyRetailPricing_UsesExplicitDiscountedPriceAndDerivesPercentage()
    {
        Product product = Product();

        policy.ApplyRetailPricing(product, 100m, 5, 80m);

        Assert.Equal(100m, product.RegularPrice);
        Assert.Equal(80m, product.DiscountedPrice);
        Assert.Equal((byte)20, product.DiscountPercentage);
    }

    [Fact]
    public void ApplyRetailPricing_UsesDiscountPercentageWhenNoExplicitDiscount()
    {
        Product product = Product();

        policy.ApplyRetailPricing(product, 100m, 25, 0m);

        Assert.Equal((byte)25, product.DiscountPercentage);
        Assert.Equal(75m, product.DiscountedPrice);
    }

    [Fact]
    public void ApplyRetailPricing_LeavesDiscountedPriceZeroWhenNoDiscount()
    {
        Product product = Product();

        policy.ApplyRetailPricing(product, 10.005m, 0, 0m);

        Assert.Equal(10.01m, product.RegularPrice);
        Assert.Equal(0m, product.DiscountedPrice);
    }

    [Fact]
    public void CreateQuote_ReturnsRetailQuote()
    {
        Product product = Product();
        product.RegularPrice = 12m;
        product.VatRate = 20m;

        var quote = policy.CreateQuote(product, 2);

        Assert.Equal(PricingTier.Retail.ToString(), quote.PricingTier);
        Assert.Equal(12m, quote.UnitPriceInclVat);
        Assert.Equal(10m, quote.UnitPriceExclVat);
        Assert.Equal(24m, quote.TotalInclVat);
        Assert.Equal(20m, quote.TotalExclVat);
        Assert.Equal(4m, quote.VatAmount);
    }

    [Fact]
    public void CreateQuote_ReturnsWholesaleQuoteWhenThresholdReached()
    {
        Product product = Product();
        product.RegularPrice = 12m;
        product.WholesalePrice = 9m;
        product.WholesaleMinQuantity = 5;
        product.VatRate = 20m;

        var quote = policy.CreateQuote(product, 5);

        Assert.Equal(PricingTier.Wholesale.ToString(), quote.PricingTier);
        Assert.Equal(9m, quote.UnitPriceInclVat);
        Assert.Equal((uint)5, quote.WholesaleMinQuantity);
    }

    private static Product Product() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "P",
            Description = "D",
            MainImageUrl = "img"
        };
}
