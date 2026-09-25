using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class OrderStockServiceTests
{
    private readonly Mock<IProductRepository> products = new();

    private OrderStockService CreateService() =>
        new(products.Object);

    [Fact]
    public async Task EnsureAvailabilityAsync_Throws409_WhenProductMissing()
    {
        Guid productId = Guid.NewGuid();

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService()
                    .EnsureAvailabilityAsync(Order(productId, 1)));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task EnsureAvailabilityAsync_Throws409_WhenStockInsufficient()
    {
        Guid productId = Guid.NewGuid();

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(Product(productId, 1));

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService()
                    .EnsureAvailabilityAsync(Order(productId, 2)));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task EnsureAvailabilityAsync_Completes_WhenStockAvailable()
    {
        Guid productId = Guid.NewGuid();

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(Product(productId, 2));

        await CreateService()
            .EnsureAvailabilityAsync(Order(productId, 2));
    }

    [Fact]
    public async Task DecreaseQuantitiesAsync_Throws404_WhenProductMissing()
    {
        Guid productId = Guid.NewGuid();

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService()
                    .DecreaseQuantitiesAsync(Order(productId, 1)));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task DecreaseQuantitiesAsync_Throws409_WhenStockInsufficient()
    {
        Guid productId = Guid.NewGuid();

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(Product(productId, 1));

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService()
                    .DecreaseQuantitiesAsync(Order(productId, 2)));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task DecreaseQuantitiesAsync_DecrementsAndPersists()
    {
        Guid productId = Guid.NewGuid();
        Product product = Product(productId, 5);

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(product);

        products
            .Setup(repository => repository.UpdateAsync(product))
            .ReturnsAsync(product);

        await CreateService()
            .DecreaseQuantitiesAsync(Order(productId, 2));

        Assert.Equal((uint)3, product.Quantity);

        products.Verify(
            repository => repository.UpdateAsync(product),
            Times.Once);
    }

    private static Order Order(
        Guid productId,
        int quantity) =>
        new()
        {
            Items =
            [
                new OrderItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    SinglePrice = 1,
                    TotalPrice = quantity,
                    Title = "P",
                    PrimaryImageUri = "p"
                }
            ]
        };

    private static Product Product(
        Guid id,
        uint quantity) =>
        new()
        {
            Id = id,
            Title = "P",
            Description = string.Empty,
            MainImageUrl = "p",
            Quantity = quantity
        };
}
