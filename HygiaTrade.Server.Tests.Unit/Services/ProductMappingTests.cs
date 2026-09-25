using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class ProductMappingTests
{
    [Fact]
    public void ToProductResponse_MapsPricingCategoryAndImages()
    {
        Product product = Product();

        var result = ProductMapping.ToProductResponse(product);

        Assert.Equal(product.Id, result.Id);
        Assert.Equal("Brand", result.Brand);
        Assert.Equal("Cat", result.CategoryName);
        Assert.True(result.WholesaleEnabled);
        Assert.Equal(100m, result.RetailPriceInclVat);
        Assert.Equal(83.33m, result.RetailPriceExclVat);
        Assert.Single(result.SecondaryImages);
    }

    [Fact]
    public void ToProductsResponse_UsesEmptyCategoryNameAndDisabledWholesale()
    {
        Product product = Product();
        product.Category = null;
        product.WholesalePrice = 0m;
        product.WholesaleMinQuantity = 0;

        var result = ProductMapping.ToProductsResponse(product);

        Assert.Equal(string.Empty, result.CategoryName);
        Assert.False(result.WholesaleEnabled);
        Assert.Equal(product.Id, result.Id);
    }

    private static Product Product() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "P",
            Brand = "Brand",
            Description = "D",
            MainImageUrl = "img",
            IsActive = true,
            RegularPrice = 100m,
            DiscountPercentage = 10,
            DiscountedPrice = 90m,
            WholesalePrice = 80m,
            WholesaleMinQuantity = 5,
            VatRate = 20m,
            Quantity = 7,
            Rating = 4.5,
            CategoryId = Guid.NewGuid(),
            Category = new Category
            {
                Name = "Cat",
                ImageUri = null
            },
            SecondaryImages =
            [
                new Image
                {
                    Id = Guid.NewGuid(),
                    Uri = "secondary"
                }
            ]
        };
}
