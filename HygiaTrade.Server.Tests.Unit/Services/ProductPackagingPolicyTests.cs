using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class ProductPackagingPolicyTests
{
    private static (Product Product, ProductPackagingOption Default, ProductPackagingOption Case) Fixtures()
    {
        var product = new Product
        {
            Title = "Sano Oxygen Baby Gel",
            Description = "",
            MainImageUrl = "image",
            Quantity = 100,
            RegularPrice = 100m
        };
        var unit = new ProductPackagingOption
        {
            ProductId = product.Id, Label = "1 л", IsDefault = true, StockQuantity = 0
        };
        var carton = new ProductPackagingOption
        {
            ProductId = product.Id, Label = "Кашон 12 × 1 л",
            UnitsPerPackage = 12, PriceInclVat = 1200m, StockQuantity = 5
        };
        product.PackagingOptions.Add(unit);
        product.PackagingOptions.Add(carton);
        return (product, unit, carton);
    }

    [Fact]
    public void Resolve_RejectsUnknownOrInactiveVariants()
    {
        var (product, _, carton) = Fixtures();
        Assert.Throws<AppException>(() => ProductPackagingPolicy.Resolve(product, Guid.NewGuid()));
        carton.IsActive = false;
        Assert.Throws<AppException>(() => ProductPackagingPolicy.Resolve(product, carton.Id));
    }

    [Fact]
    public void DefaultVariant_UsesExistingProductStock()
    {
        var (product, unit, _) = Fixtures();
        Assert.Equal((uint)100, ProductPackagingPolicy.Available(product, unit));
        ProductPackagingPolicy.Take(product, unit, 3);
        Assert.Equal((uint)97, product.Quantity);
        Assert.Equal((uint)0, unit.StockQuantity);
    }

    [Fact]
    public void Carton_UsesItsOwnStockAndCannotOversell()
    {
        var (product, _, carton) = Fixtures();
        Assert.Equal((uint)5, ProductPackagingPolicy.Available(product, carton));
        ProductPackagingPolicy.Take(product, carton, 4);
        Assert.Equal((uint)1, carton.StockQuantity);
        Assert.Equal((uint)100, product.Quantity);
        Assert.Throws<AppException>(() => ProductPackagingPolicy.Take(product, carton, 2));
        ProductPackagingPolicy.Restore(product, carton, 4);
        Assert.Equal((uint)5, carton.StockQuantity);
    }

    [Fact]
    public void OptionPrice_IsUsedForTotalsAndVat()
    {
        var (product, _, carton) = Fixtures();
        product.VatRate = 20m;
        var item = new OrderItem
        {
            ProductId = product.Id, PackagingOptionId = carton.Id,
            Quantity = 2, SinglePrice = 0m, TotalPrice = 0m,
            Title = product.Title, PrimaryImageUri = product.MainImageUrl
        };
        var service = new OrderPricingService(
            new Moq.Mock<HygiaTrade.Data.Interfaces.IProductRepository>().Object);
        service.ApplyCurrentPricing(item, product, 2);
        Assert.Equal(1200m, item.SinglePrice);
        Assert.Equal(2400m, item.TotalPrice);
        Assert.Equal(2000m, item.TotalPriceExclVat);
        Assert.Equal(400m, item.VatAmount);
    }
}
