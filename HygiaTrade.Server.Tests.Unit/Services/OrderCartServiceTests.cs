using HygiaTrade.Common.Requests.OrderItem;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class OrderCartServiceTests
{
    private readonly Mock<IAuthService> auth = new();
    private readonly Mock<IOrderRepository> orders = new();
    private readonly Mock<IProductRepository> products = new();
    private readonly Mock<IOrderItemRepository> items = new();
    private readonly Mock<IOrderPricingService> pricing = new();

    private OrderCartService CreateService() =>
        new(
            auth.Object,
            orders.Object,
            products.Object,
            items.Object,
            pricing.Object);

    [Fact]
    public async Task GetAsync_Throws404_WhenOrderMissing()
    {
        SetupUser();

        orders
            .Setup(repository => repository.GetByUserIdAsync(
                It.IsAny<Guid>()))
            .ReturnsAsync((Order?)null);

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().GetAsync());

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task GetAsync_ReturnsMappedOrder()
    {
        Guid userId = SetupUser();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderTotalPrice = 10m
        };

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(order);

        var result = await CreateService().GetAsync();

        Assert.Equal(order.Id, result.Id);
    }

    [Fact]
    public async Task AddProductAsync_CreatesCartAndAddsNewItem()
    {
        Guid userId = SetupUser();
        Guid productId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };

        Product product = Product(productId, 10);

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync((Order?)null);

        orders
            .Setup(repository => repository.AddAsync(userId))
            .ReturnsAsync(order);

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(product);

        items
            .Setup(repository => repository.AddAsync(
                It.IsAny<OrderItem>()))
            .ReturnsAsync((OrderItem item) => item);

        orders
            .Setup(repository => repository.UpdateAsync(order))
            .ReturnsAsync(order);

        var result =
            await CreateService().AddProductAsync(
                new AddOrderItemRequest
                {
                    ProductId = productId,
                    Quantity = 2
                });

        Assert.Single(order.Items);

        pricing.Verify(
            service => service.ApplyCurrentPricing(
                It.Is<OrderItem>(item =>
                    item.ProductId == productId),
                product,
                2),
            Times.Once);

        pricing.Verify(
            service => service.UpdateOrderPrices(order),
            Times.Once);

        Assert.Equal(order.Id, result.Id);
    }

    [Fact]
    public async Task AddProductAsync_Throws404_WhenProductMissing()
    {
        SetupUser();

        products
            .Setup(repository => repository.GetByIdAsync(
                It.IsAny<Guid>()))
            .ReturnsAsync((Product?)null);

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().AddProductAsync(
                    new AddOrderItemRequest
                    {
                        ProductId = Guid.NewGuid(),
                        Quantity = 1
                    }));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task AddProductAsync_RejectsNonPositiveResultQuantity()
    {
        Guid userId = SetupUser();
        Guid productId = Guid.NewGuid();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Items =
            [
                new OrderItem
                {
                    ProductId = productId,
                    Quantity = 1,
                    SinglePrice = 1,
                    TotalPrice = 1,
                    Title = "P",
                    PrimaryImageUri = "p"
                }
            ]
        };

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(order);

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(Product(productId, 10));

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().AddProductAsync(
                    new AddOrderItemRequest
                    {
                        ProductId = productId,
                        Quantity = -1
                    }));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task AddProductAsync_RejectsInsufficientStock()
    {
        Guid userId = SetupUser();
        Guid productId = Guid.NewGuid();

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(new Order { Id = Guid.NewGuid() });

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(Product(productId, 1));

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().AddProductAsync(
                    new AddOrderItemRequest
                    {
                        ProductId = productId,
                        Quantity = 2
                    }));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task AddProductAsync_RepricesExistingItem()
    {
        Guid userId = SetupUser();
        Guid productId = Guid.NewGuid();
        Product product = Product(productId, 10);
        var existing = new OrderItem
        {
            ProductId = productId,
            Quantity = 2,
            SinglePrice = 1,
            TotalPrice = 2,
            Title = "P",
            PrimaryImageUri = "p"
        };
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Items = [existing]
        };

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(order);

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(product);

        orders
            .Setup(repository => repository.UpdateAsync(order))
            .ReturnsAsync(order);

        await CreateService().AddProductAsync(
            new AddOrderItemRequest
            {
                ProductId = productId,
                Quantity = 3
            });

        pricing.Verify(
            service => service.ApplyCurrentPricing(
                existing,
                product,
                5),
            Times.Once);

        items.Verify(
            repository => repository.AddAsync(
                It.IsAny<OrderItem>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveProductAsync_Throws404_WhenOrderMissing()
    {
        SetupUser();

        orders
            .Setup(repository => repository.GetByUserIdAsync(
                It.IsAny<Guid>()))
            .ReturnsAsync((Order?)null);

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().RemoveProductAsync(
                    Remove(Guid.NewGuid(), 1)));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task RemoveProductAsync_Throws404_WhenItemMissing()
    {
        Guid userId = SetupUser();

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(new Order { Id = Guid.NewGuid() });

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().RemoveProductAsync(
                    Remove(Guid.NewGuid(), 1)));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task RemoveProductAsync_DeletesEmptyCartAndReturns200Exception()
    {
        Guid userId = SetupUser();
        Guid productId = Guid.NewGuid();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Items =
            [
                Item(productId, 1)
            ]
        };

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(order);

        orders
            .Setup(repository => repository.DeleteAsync(order.Id))
            .ReturnsAsync(true);

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().RemoveProductAsync(
                    Remove(productId, 1)));

        Assert.Equal(200, exception.StatusCode);
        Assert.Empty(order.Items);
    }

    [Fact]
    public async Task RemoveProductAsync_Throws404_WhenRemainingItemProductMissing()
    {
        Guid userId = SetupUser();
        Guid productId = Guid.NewGuid();

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Items =
            [
                Item(productId, 2)
            ]
        };

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(order);

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().RemoveProductAsync(
                    Remove(productId, 1)));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task RemoveProductAsync_RepricesAndUpdatesRemainingItem()
    {
        Guid userId = SetupUser();
        Guid productId = Guid.NewGuid();
        Product product = Product(productId, 10);
        OrderItem item = Item(productId, 3);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Items = [item]
        };

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(order);

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync(product);

        orders
            .Setup(repository => repository.UpdateAsync(order))
            .ReturnsAsync(order);

        await CreateService().RemoveProductAsync(
            Remove(productId, 1));

        pricing.Verify(
            service => service.ApplyCurrentPricing(
                item,
                product,
                2),
            Times.Once);

        pricing.Verify(
            service => service.UpdateOrderPrices(order),
            Times.Once);
    }

    private Guid SetupUser()
    {
        Guid id = Guid.NewGuid();

        auth
            .Setup(service => service.GetCurrentUserId())
            .ReturnsAsync(id.ToString());

        return id;
    }

    private static RemoveOrderItemRequest Remove(
        Guid productId,
        int quantity) =>
        new()
        {
            ProductId = productId,
            Quantity = quantity
        };

    private static OrderItem Item(
        Guid productId,
        int quantity) =>
        new()
        {
            ProductId = productId,
            Quantity = quantity,
            SinglePrice = 1,
            TotalPrice = quantity,
            Title = "P",
            PrimaryImageUri = "p"
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
