using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class ProductReadServiceTests
{
    private readonly Mock<IProductRepository> products = new();
    private readonly Mock<IProductPricingPolicy> pricing = new();

    private ProductReadService CreateService() =>
        new(products.Object, pricing.Object);

    [Fact]
    public async Task GetAsync_MapsProducts()
    {
        products.Setup(x => x.GetAllAsync())
            .ReturnsAsync([Product("A"), Product("B")]);

        var result = (await CreateService().GetAsync())!.ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Title);
        Assert.Equal("B", result[1].Title);
    }

    [Fact]
    public async Task GetBestSellersAsync_MapsProducts()
    {
        products.Setup(x => x.GetBestSellersAsync(2))
            .ReturnsAsync([Product("A"), Product("B")]);

        var result = (await CreateService().GetBestSellersAsync(2))!.ToList();

        Assert.Equal(2, result.Count);
        products.Verify(x => x.GetBestSellersAsync(2), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_Throws404_WhenMissing()
    {
        products.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Product?)null);

        AppException ex = await Assert.ThrowsAsync<AppException>(
            () => CreateService().GetByIdAsync(Guid.NewGuid()));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedProduct()
    {
        Product product = Product("A");
        products.Setup(x => x.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var result = await CreateService().GetByIdAsync(product.Id);

        Assert.Equal(product.Id, result!.Id);
        Assert.Equal("A", result.Title);
    }

    [Fact]
    public async Task GetPriceQuoteAsync_RejectsNonPositiveQuantityBeforeLookup()
    {
        AppException ex = await Assert.ThrowsAsync<AppException>(
            () => CreateService().GetPriceQuoteAsync(Guid.NewGuid(), 0));

        Assert.Equal(400, ex.StatusCode);
        products.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetPriceQuoteAsync_Throws404_WhenProductMissing()
    {
        products.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Product?)null);

        AppException ex = await Assert.ThrowsAsync<AppException>(
            () => CreateService().GetPriceQuoteAsync(Guid.NewGuid(), 1));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task GetPriceQuoteAsync_DelegatesToPricingPolicy()
    {
        Product product = Product("A");
        var expected = new HygiaTrade.Common.Responses.Product.ProductPriceQuoteResponse
        {
            ProductId = product.Id,
            Quantity = 4
        };

        products.Setup(x => x.GetByIdAsync(product.Id)).ReturnsAsync(product);
        pricing.Setup(x => x.CreateQuote(product, 4)).Returns(expected);

        Assert.Same(expected, await CreateService().GetPriceQuoteAsync(product.Id, 4));
    }

    private static Product Product(string title) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "D",
            MainImageUrl = "img",
            VatRate = 20m
        };
}
