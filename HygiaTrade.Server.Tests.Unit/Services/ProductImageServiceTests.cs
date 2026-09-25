using HygiaTrade.Common.Requests.Image;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class ProductImageServiceTests
{
    private readonly Mock<IImageRepository> images = new();

    private ProductImageService CreateService() => new(images.Object);

    [Fact]
    public async Task AddAsync_CreatesAndPersistsImages()
    {
        var product = Product();
        CreateImageRequest[] requests =
        [
            new() { Uri = "a" },
            new() { Uri = "b" }
        ];

        images.Setup(x => x.AddAsync(It.IsAny<Image>()))
            .ReturnsAsync((Image image) => image);

        List<Image> result = await CreateService().AddAsync(product, requests);

        Assert.Equal(2, result.Count);
        Assert.All(result, image => Assert.Equal(product.Id, image.ProductId));
        images.Verify(x => x.AddAsync(It.IsAny<Image>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ReplaceAsync_DeletesOldAndAddsNew()
    {
        var product = Product();
        product.SecondaryImages =
        [
            new Image { Id = Guid.NewGuid(), Uri = "old", ProductId = product.Id }
        ];

        images.Setup(x => x.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        images.Setup(x => x.AddAsync(It.IsAny<Image>()))
            .ReturnsAsync((Image image) => image);

        await CreateService().ReplaceAsync(
            product,
            [new UpdateImageRequest { Uri = "new" }]);

        Assert.Single(product.SecondaryImages);
        Assert.Equal("new", product.SecondaryImages.Single().Uri);
        images.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Once);
        images.Verify(x => x.AddAsync(It.IsAny<Image>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAllAsync_DeletesEachImage()
    {
        var product = Product();
        product.SecondaryImages =
        [
            new Image { Id = Guid.NewGuid(), Uri = "a" },
            new Image { Id = Guid.NewGuid(), Uri = "b" }
        ];

        images.Setup(x => x.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        await CreateService().DeleteAllAsync(product);

        images.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Exactly(2));
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
