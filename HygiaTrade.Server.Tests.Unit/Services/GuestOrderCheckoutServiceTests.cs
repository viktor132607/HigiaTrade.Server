using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class GuestOrderCheckoutServiceTests
{
    private readonly Mock<IGuestOrderRepository> repository = new();
    private readonly Mock<IOrderPricingService> pricing = new();

    private GuestOrderCheckoutService CreateService() =>
        new(repository.Object, pricing.Object);

    [Fact]
    public async Task SendAsync_RequiresConsent()
    {
        var request = Request();
        request.ConsentAccepted = false;

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().SendAsync(request));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_RejectsEmptyCart()
    {
        var request = Request();
        request.ConsentAccepted = true;
        request.Items = [];

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().SendAsync(request));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_RejectsUnavailableProducts()
    {
        var request = Request();
        request.ConsentAccepted = true;

        repository
            .Setup(item => item.GetAvailableProductsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, Product>());

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().SendAsync(request));

        Assert.Equal(409, exception.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task SendAsync_RejectsInvalidOrInsufficientQuantity(
        int quantity)
    {
        Guid productId = Guid.NewGuid();
        var request = Request(productId, quantity);
        request.ConsentAccepted = true;

        repository
            .Setup(item => item.GetAvailableProductsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(
                new Dictionary<Guid, Product>
                {
                    [productId] = Product(productId, 2)
                });

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().SendAsync(request));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_BuildsPricesDecrementsStockAndPersists()
    {
        Guid productId = Guid.NewGuid();
        var request = Request(productId, 2);
        request.ConsentAccepted = true;
        request.Names = " Guest ";
        request.Email = " guest@example.com ";
        Product product = Product(productId, 5);

        repository
            .Setup(item => item.GetAvailableProductsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>()))
            .ReturnsAsync(
                new Dictionary<Guid, Product>
                {
                    [productId] = product
                });

        Guid expected = Guid.NewGuid();

        repository
            .Setup(item => item.SaveAsync(
                It.IsAny<Order>(),
                It.IsAny<IReadOnlyCollection<Product>>()))
            .ReturnsAsync(expected);

        Guid result =
            await CreateService().SendAsync(request);

        Assert.Equal(expected, result);
        Assert.Equal((uint)3, product.Quantity);

        pricing.Verify(
            service => service.ApplyCurrentPricing(
                It.Is<OrderItem>(item =>
                    item.ProductId == productId &&
                    item.Quantity == 2),
                product,
                2),
            Times.Once);

        pricing.Verify(
            service => service.UpdateOrderPrices(
                It.Is<Order>(order =>
                    order.GuestEmail == "guest@example.com" &&
                    order.Names == "Guest" &&
                    order.Items.Count == 1)),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_UsesDistinctProductIdsForLookup()
    {
        Guid productId = Guid.NewGuid();
        var request = Request(productId, 1);
        request.ConsentAccepted = true;
        request.Items.Add(
            new GuestOrderItemRequest
            {
                ProductId = productId,
                Quantity = 1
            });

        Product product = Product(productId, 5);

        repository
            .Setup(item => item.GetAvailableProductsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>()))
            .Callback<IReadOnlyCollection<Guid>>(ids =>
                Assert.Single(ids))
            .ReturnsAsync(
                new Dictionary<Guid, Product>
                {
                    [productId] = product
                });

        repository
            .Setup(item => item.SaveAsync(
                It.IsAny<Order>(),
                It.IsAny<IReadOnlyCollection<Product>>()))
            .ReturnsAsync(Guid.NewGuid());

        await CreateService().SendAsync(request);

        Assert.Equal((uint)3, product.Quantity);
    }

    private static GuestOrderRequest Request(
        Guid? productId = null,
        int quantity = 1) =>
        new()
        {
            Names = "Guest",
            Email = "guest@example.com",
            PostalCode = "7000",
            Country = "BG",
            City = "Ruse",
            Address = "Street",
            Phone = "1",
            Items =
            [
                new GuestOrderItemRequest
                {
                    ProductId = productId ?? Guid.NewGuid(),
                    Quantity = quantity
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
            Quantity = quantity,
            RegularPrice = 10m
        };
}
