using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Core.Pages;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Data.PaginationAndFiltering;
using HygiaTrade.Domain.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class OrderAdministrationServiceTests
{
    private readonly Mock<IOrderRepository> orders = new();
    private readonly Mock<IAuthService> auth = new();
    private readonly Mock<IUserRepository> users = new();
    private readonly Mock<IEmailNotificationService> email = new();
    private readonly Mock<IOrderStockService> stock = new();

    private OrderAdministrationService CreateService() =>
        new(
            orders.Object,
            auth.Object,
            users.Object,
            email.Object,
            stock.Object);

    [Fact]
    public async Task ChangeStatusAsync_Throws404_WhenOrderMissing()
    {
        orders
            .Setup(repository => repository.GetByIdAsync(
                It.IsAny<Guid>()))
            .ReturnsAsync((Order?)null);

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().ChangeStatusAsync(
                    new ChangeOrderStatusRequest
                    {
                        OrderId = Guid.NewGuid(),
                        OrderStatus = OrderStatus.Cancelled
                    }));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task ChangeStatusAsync_ConsumesStock_ForCreatedNonCancelledOrder()
    {
        Guid userId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.Created
        };

        var user = User(userId);

        orders
            .Setup(repository => repository.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        orders
            .Setup(repository => repository.ChangeStatusAsync(
                order.Id,
                OrderStatus.Processing))
            .ReturnsAsync(order);

        users
            .Setup(repository => repository.GetByIdAsync(userId))
            .ReturnsAsync(user);

        bool result =
            await CreateService().ChangeStatusAsync(
                new ChangeOrderStatusRequest
                {
                    OrderId = order.Id,
                    OrderStatus = OrderStatus.Processing
                });

        Assert.True(result);

        stock.Verify(
            service => service.EnsureAvailabilityAsync(order),
            Times.Once);

        stock.Verify(
            service => service.DecreaseQuantitiesAsync(order),
            Times.Once);

        email.Verify(
            service => service.SendOrderStatusChangedAsync(
                user,
                order),
            Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_SkipsStock_WhenCancelled()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Created
        };

        orders
            .Setup(repository => repository.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        orders
            .Setup(repository => repository.ChangeStatusAsync(
                order.Id,
                OrderStatus.Cancelled))
            .ReturnsAsync(order);

        bool result =
            await CreateService().ChangeStatusAsync(
                new ChangeOrderStatusRequest
                {
                    OrderId = order.Id,
                    OrderStatus = OrderStatus.Cancelled
                });

        Assert.True(result);

        stock.Verify(
            service => service.EnsureAvailabilityAsync(
                It.IsAny<Order>()),
            Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_SkipsEmail_WhenUserMissing()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Processing
        };

        orders
            .Setup(repository => repository.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        orders
            .Setup(repository => repository.ChangeStatusAsync(
                order.Id,
                OrderStatus.Shipped))
            .ReturnsAsync(order);

        users
            .Setup(repository => repository.GetByIdAsync(
                It.IsAny<Guid?>()))
            .ReturnsAsync((User?)null);

        Assert.True(
            await CreateService().ChangeStatusAsync(
                new ChangeOrderStatusRequest
                {
                    OrderId = order.Id,
                    OrderStatus = OrderStatus.Shipped
                }));

        email.Verify(
            service => service.SendOrderStatusChangedAsync(
                It.IsAny<User>(),
                It.IsAny<Order>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchOrdersAsync_Throws403_ForNonAdminGlobalSearch()
    {
        auth
            .Setup(service => service.GetCurrentUserRole())
            .ReturnsAsync("User");

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().SearchOrdersAsync(
                    new SearchOrderRequest()));

        Assert.Equal(403, exception.StatusCode);
    }

    [Fact]
    public async Task SearchOrdersAsync_MapsAdminSearchAndDefaults()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderTotalPrice = 10m,
            Items =
            [
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    SinglePrice = 10m,
                    TotalPrice = 10m,
                    Title = "B",
                    PrimaryImageUri = "b"
                }
            ]
        };

        auth
            .Setup(service => service.GetCurrentUserRole())
            .ReturnsAsync(Roles.Admin);

        Filter<Order>? captured = null;

        orders
            .Setup(repository => repository.SearchAsync(
                It.IsAny<Filter<Order>>()))
            .Callback<Filter<Order>>(filter => captured = filter)
            .ReturnsAsync(
                new Paginated<Order>
                {
                    Items = [order],
                    TotalCount = 1
                });

        var result =
            await CreateService().SearchOrdersAsync(
                new SearchOrderRequest());

        Assert.Single(result.Items!);
        Assert.Equal(order.Id, result.Items!.Single().Id);
        Assert.Equal(1, result.TotalCount);
        Assert.NotNull(captured);
        Assert.Equal(1, captured!.PageNumber);
        Assert.Equal(10, captured.PageSize);
        Assert.Equal("CreatedOn", captured.SortBy);
        Assert.False(captured.SortDescending);
    }

    [Fact]
    public async Task SearchOrdersAsync_WithUserId_DoesNotRequireAdminRole()
    {
        Guid userId = Guid.NewGuid();

        orders
            .Setup(repository => repository.SearchAsync(
                It.IsAny<Filter<Order>>()))
            .ReturnsAsync(
                new Paginated<Order>
                {
                    Items = [],
                    TotalCount = 0
                });

        await CreateService().SearchOrdersAsync(
            new SearchOrderRequest
            {
                UserId = userId
            });

        auth.Verify(
            service => service.GetCurrentUserRole(),
            Times.Never);
    }

    private static User User(Guid id) =>
        new()
        {
            Id = id,
            Email = "user@example.com",
            Names = "User",
            Phone = "1"
        };
}
