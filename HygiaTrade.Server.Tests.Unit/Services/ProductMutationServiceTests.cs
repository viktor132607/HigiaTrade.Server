using HygiaTrade.Common.Requests.Image;
using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class ProductMutationServiceTests
{
    private readonly Mock<IProductRepository> products = new();
    private readonly Mock<ICategoryRepository> categories = new();
    private readonly Mock<IProductImageService> images = new();
    private readonly Mock<IProductPricingPolicy> pricing = new();

    private ProductMutationService CreateService() =>
        new(products.Object, categories.Object, images.Object, pricing.Object);

    [Fact]
    public async Task CreateAsync_Throws400_WhenCategoryMissing()
    {
        var request = CreateRequest();
        categories.Setup(x => x.GetByIdAsync(request.CategoryId))
            .ReturnsAsync((Category?)null);

        AppException ex = await Assert.ThrowsAsync<AppException>(
            () => CreateService().CreateAsync(request));

        Assert.Equal(400, ex.StatusCode);
        pricing.Verify(x => x.Validate(
            It.IsAny<decimal>(), It.IsAny<byte>(), It.IsAny<decimal>(),
            It.IsAny<decimal>(), It.IsAny<uint>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ThrowsWhenPersistenceReturnsNull()
    {
        var request = CreateRequest();
        Category category = Category();

        categories.Setup(x => x.GetByIdAsync(request.CategoryId)).ReturnsAsync(category);
        products.Setup(x => x.AddAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_NormalizesBrandAndAddsImages()
    {
        var request = CreateRequest();
        request.Brand = " Brand ";
        request.WholesalePrice = 7.555m;
        request.WholesaleMinQuantity = 5;
        Category category = Category();

        categories.Setup(x => x.GetByIdAsync(request.CategoryId)).ReturnsAsync(category);
        products.Setup(x => x.AddAsync(It.IsAny<Product>()))
            .ReturnsAsync((Product product) => product);
        images.Setup(x => x.AddAsync(It.IsAny<Product>(), request.SecondaryImages))
            .ReturnsAsync([new Image { Uri = "secondary" }]);

        var result = await CreateService().CreateAsync(request);

        Assert.Equal("Brand", result!.Brand);
        Assert.Equal((uint)0, result.Quantity);
        Assert.Equal(0, result.Rating);
        Assert.Equal("Cat", result.CategoryName);

        pricing.Verify(x => x.Validate(
            request.RegularPrice,
            request.DiscountPercentage,
            request.DiscountedPrice,
            request.WholesalePrice,
            request.WholesaleMinQuantity,
            request.VatRate), Times.Once);

        pricing.Verify(x => x.ApplyRetailPricing(
            It.Is<Product>(p => p.Brand == "Brand" && p.WholesalePrice == 7.56m),
            request.RegularPrice,
            request.DiscountPercentage,
            request.DiscountedPrice), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ConvertsWhitespaceBrandToNull()
    {
        var request = CreateRequest();
        request.Brand = "   ";
        Category category = Category();

        categories.Setup(x => x.GetByIdAsync(request.CategoryId)).ReturnsAsync(category);
        products.Setup(x => x.AddAsync(It.IsAny<Product>()))
            .Callback<Product>(p => Assert.Null(p.Brand))
            .ReturnsAsync((Product product) => product);
        images.Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<IEnumerable<CreateImageRequest>>()))
            .ReturnsAsync([]);

        await CreateService().CreateAsync(request);
    }

    [Fact]
    public async Task UpdateAsync_Throws404_WhenProductMissing()
    {
        var request = UpdateRequest();
        products.Setup(x => x.GetByIdAsync(request.Id)).ReturnsAsync((Product?)null);

        AppException ex = await Assert.ThrowsAsync<AppException>(
            () => CreateService().UpdateAsync(request));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_Throws400_WhenCategoryMissing()
    {
        var request = UpdateRequest();
        products.Setup(x => x.GetByIdAsync(request.Id)).ReturnsAsync(Product(request.Id));
        categories.Setup(x => x.GetByIdAsync(request.CategoryId)).ReturnsAsync((Category?)null);

        AppException ex = await Assert.ThrowsAsync<AppException>(
            () => CreateService().UpdateAsync(request));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_PreservesOptionalWholesaleAndVat_WhenOmitted()
    {
        var request = UpdateRequest();
        request.WholesalePrice = null;
        request.WholesaleMinQuantity = null;
        request.VatRate = null;

        Product existing = Product(request.Id);
        existing.WholesalePrice = 8m;
        existing.WholesaleMinQuantity = 4;
        existing.VatRate = 9m;
        Category category = Category();

        products.Setup(x => x.GetByIdAsync(request.Id)).ReturnsAsync(existing);
        categories.Setup(x => x.GetByIdAsync(request.CategoryId)).ReturnsAsync(category);
        images.Setup(x => x.ReplaceAsync(existing, request.SecondaryImages)).Returns(Task.CompletedTask);
        products.Setup(x => x.UpdateAsync(existing)).ReturnsAsync(existing);

        await CreateService().UpdateAsync(request);

        pricing.Verify(x => x.Validate(
            request.RegularPrice,
            request.DiscountPercentage,
            request.DiscountedPrice,
            8m, 4, 9m), Times.Once);

        Assert.Equal(8m, existing.WholesalePrice);
        Assert.Equal((uint)4, existing.WholesaleMinQuantity);
        Assert.Equal(9m, existing.VatRate);
    }

    [Fact]
    public async Task UpdateAsync_UsesProvidedOptionalValuesAndNormalizesBrand()
    {
        var request = UpdateRequest();
        request.Brand = " New ";
        request.WholesalePrice = 6.789m;
        request.WholesaleMinQuantity = 3;
        request.VatRate = 20m;

        Product existing = Product(request.Id);
        Category category = Category();

        products.Setup(x => x.GetByIdAsync(request.Id)).ReturnsAsync(existing);
        categories.Setup(x => x.GetByIdAsync(request.CategoryId)).ReturnsAsync(category);
        images.Setup(x => x.ReplaceAsync(existing, request.SecondaryImages)).Returns(Task.CompletedTask);
        products.Setup(x => x.UpdateAsync(existing)).ReturnsAsync(existing);

        var result = await CreateService().UpdateAsync(request);

        Assert.Equal("New", existing.Brand);
        Assert.Equal(6.79m, existing.WholesalePrice);
        Assert.Equal((uint)3, existing.WholesaleMinQuantity);
        Assert.Equal(20m, existing.VatRate);
        Assert.Equal("Cat", result!.CategoryName);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenPersistenceReturnsNull()
    {
        var request = UpdateRequest();
        Product existing = Product(request.Id);

        products.Setup(x => x.GetByIdAsync(request.Id)).ReturnsAsync(existing);
        categories.Setup(x => x.GetByIdAsync(request.CategoryId)).ReturnsAsync(Category());
        images.Setup(x => x.ReplaceAsync(existing, request.SecondaryImages)).Returns(Task.CompletedTask);
        products.Setup(x => x.UpdateAsync(existing)).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().UpdateAsync(request));
    }

    [Fact]
    public async Task DeleteAsync_Throws404_WhenMissing()
    {
        products.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product?)null);

        AppException ex = await Assert.ThrowsAsync<AppException>(
            () => CreateService().DeleteAsync(Guid.NewGuid()));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_DeletesImagesThenProduct()
    {
        Product product = Product(Guid.NewGuid());

        products.Setup(x => x.GetByIdAsync(product.Id)).ReturnsAsync(product);
        images.Setup(x => x.DeleteAllAsync(product)).Returns(Task.CompletedTask);
        products.Setup(x => x.DeleteAsync(product.Id)).ReturnsAsync(true);

        Assert.True(await CreateService().DeleteAsync(product.Id));
        images.Verify(x => x.DeleteAllAsync(product), Times.Once);
        products.Verify(x => x.DeleteAsync(product.Id), Times.Once);
    }

    private static CreateProductRequest CreateRequest() =>
        new()
        {
            Title = "P",
            Description = "D",
            MainImageUrl = "img",
            CategoryId = Guid.NewGuid(),
            RegularPrice = 10m,
            VatRate = 20m,
            SecondaryImages = [new CreateImageRequest { Uri = "secondary" }]
        };

    private static UpdateProductRequest UpdateRequest() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "Updated",
            Description = "D2",
            MainImageUrl = "img2",
            CategoryId = Guid.NewGuid(),
            RegularPrice = 20m,
            VatRate = 20m,
            SecondaryImages = [new UpdateImageRequest { Uri = "new" }]
        };

    private static Category Category() =>
        new()
        {
            Name = "Cat",
            ImageUri = null
        };

    private static Product Product(Guid id) =>
        new()
        {
            Id = id,
            Title = "P",
            Description = "D",
            MainImageUrl = "img",
            SecondaryImages = []
        };
}
