using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class CurrentOrderCheckoutServiceTests
{
    private readonly Mock<IAuthService> auth = new();
    private readonly Mock<IOrderRepository> orders = new();
    private readonly Mock<IUserRepository> users = new();
    private readonly Mock<IEmailNotificationService> email = new();
    private readonly Mock<IOrderPricingService> pricing = new();
    private readonly Mock<IOrderStockService> stock = new();

    private CurrentOrderCheckoutService CreateService() =>
        new(
            auth.Object,
            orders.Object,
            users.Object,
            email.Object,
            pricing.Object,
            stock.Object);

    [Fact]
    public async Task SendAsync_Throws404_WhenOrderMissing()
    {
        Guid userId = SetupUser();

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync((Order?)null);

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().SendAsync(Request()));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_Throws404_WhenCartEmpty()
    {
        Guid userId = SetupUser();

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(new Order());

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().SendAsync(Request()));

        Assert.Equal(404, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_RequiresConsent()
    {
        Guid userId = SetupUser();

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(OrderWithItem());

        var request = Request();
        request.ConsentAccepted = false;

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().SendAsync(request));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task SendAsync_ChecksPricingStockAndSendsConfirmation()
    {
        Guid userId = SetupUser();
        Order order = OrderWithItem();
        User user = User(userId);
        var request = Request();
        request.ConsentAccepted = true;
        request.PaymentMethod = " bank-transfer ";
        request.DeliveryMethod = " regional-delivery ";

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(order);

        orders
            .Setup(repository => repository.UpdateAsync(order))
            .ReturnsAsync(order);

        users
            .Setup(repository => repository.GetByIdAsync(userId))
            .ReturnsAsync(user);

        bool result =
            await CreateService().SendAsync(request);

        Assert.True(result);
        Assert.Equal(OrderStatus.PendingVerification, order.Status);
        Assert.Equal(request.Names, order.Names);

        pricing.Verify(
            service => service.RefreshCurrentCartPricingAsync(order),
            Times.Once);

        stock.Verify(
            service => service.EnsureAvailabilityAsync(order),
            Times.Once);

        stock.Verify(
            service => service.DecreaseQuantitiesAsync(order),
            Times.Once);

        email.Verify(
            service => service.SendOrderConfirmationAsync(
                user,
                order,
                "bank-transfer",
                "regional-delivery"),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_SkipsEmailWithoutUser()
    {
        Guid userId = SetupUser();
        Order order = OrderWithItem();
        var request = Request();
        request.ConsentAccepted = true;
        request.DeliveryMethod = "regional-delivery";

        orders
            .Setup(repository => repository.GetByUserIdAsync(userId))
            .ReturnsAsync(order);

        orders
            .Setup(repository => repository.UpdateAsync(order))
            .ReturnsAsync(order);

        users
            .Setup(repository => repository.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        Assert.True(
            await CreateService().SendAsync(request));

        email.Verify(
            service => service.SendOrderConfirmationAsync(
                It.IsAny<User>(),
                It.IsAny<Order>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_RejectsBelowMinimumBeforeSavingOrReducingStock()
    {
        Guid userId = SetupUser();
        var order = OrderWithItem(); order.OrderTotalPrice = 49.99m;
        orders.Setup(repository => repository.GetByUserIdAsync(userId)).ReturnsAsync(order);
        var request = Request(); request.ConsentAccepted = true;
        await Assert.ThrowsAsync<AppException>(() => CreateService().SendAsync(request));
        orders.Verify(repository => repository.UpdateAsync(It.IsAny<Order>()), Times.Never);
        stock.Verify(service => service.DecreaseQuantitiesAsync(It.IsAny<Order>()), Times.Never);
    }

    private Guid SetupUser()
    {
        Guid id = Guid.NewGuid();

        auth
            .Setup(service => service.GetCurrentUserId())
            .ReturnsAsync(id.ToString());

        return id;
    }

    private static Order OrderWithItem() =>
        new()
        {
            OrderTotalPrice = 50m,
            Items =
            [
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    SinglePrice = 10m,
                    TotalPrice = 10m,
                    Title = "P",
                    PrimaryImageUri = "p"
                }
            ]
        };

    private static SendOrderRequest Request() =>
        new()
        {
            PaymentMethod = "cash-on-delivery",
            DeliveryMethod = "regional-delivery",
            Names = "User",
            PostalCode = "7000",
            Country = "BG",
            City = "Ruse",
            Address = "Street",
            Phone = "1"
        };

    private static User User(Guid id) =>
        new()
        {
            Id = id,
            Email = "user@example.com",
            Names = "User",
            Phone = "1"
        };
}
