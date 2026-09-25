using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Common.Requests.OrderItem;
using HygiaTrade.Common.Responses.Order;
using HygiaTrade.Core.Pages;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class OrderServiceTests
{
    private readonly Mock<IOrderAdministrationService> administration = new();
    private readonly Mock<IOrderCartService> cart = new();
    private readonly Mock<ICurrentOrderCheckoutService> currentCheckout = new();
    private readonly Mock<IGuestOrderCheckoutService> guestCheckout = new();

    private OrderService CreateService() =>
        new(
            administration.Object,
            cart.Object,
            currentCheckout.Object,
            guestCheckout.Object);

    [Fact]
    public async Task ChangeStatusAsync_Delegates()
    {
        var request = new ChangeOrderStatusRequest
        {
            OrderId = Guid.NewGuid(),
            OrderStatus = Core.Enums.OrderStatus.Processing
        };

        administration
            .Setup(service => service.ChangeStatusAsync(request))
            .ReturnsAsync(true);

        Assert.True(
            await CreateService().ChangeStatusAsync(request));
    }

    [Fact]
    public async Task SearchOrdersAsync_Delegates()
    {
        var request = new SearchOrderRequest();
        var expected = new Paginated<OrderResponse>
        {
            Items = [],
            TotalCount = 0
        };

        administration
            .Setup(service => service.SearchOrdersAsync(request))
            .ReturnsAsync(expected);

        Assert.Same(
            expected,
            await CreateService().SearchOrdersAsync(request));
    }

    [Fact]
    public async Task GetAsync_Delegates()
    {
        var expected = Response();

        cart.Setup(service => service.GetAsync())
            .ReturnsAsync(expected);

        Assert.Same(expected, await CreateService().GetAsync());
    }

    [Fact]
    public async Task AddProductAsync_Delegates()
    {
        var request = new AddOrderItemRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity = 1
        };

        var expected = Response();

        cart
            .Setup(service => service.AddProductAsync(request))
            .ReturnsAsync(expected);

        Assert.Same(
            expected,
            await CreateService().AddProductAsync(request));
    }

    [Fact]
    public async Task RemoveProductAsync_Delegates()
    {
        var request = new RemoveOrderItemRequest
        {
            ProductId = Guid.NewGuid(),
            Quantity = 1
        };

        var expected = Response();

        cart
            .Setup(service => service.RemoveProductAsync(request))
            .ReturnsAsync(expected);

        Assert.Same(
            expected,
            await CreateService().RemoveProductAsync(request));
    }

    [Fact]
    public async Task SendCurrentAsync_Delegates()
    {
        var request = SendRequest();

        currentCheckout
            .Setup(service => service.SendAsync(request))
            .ReturnsAsync(true);

        Assert.True(
            await CreateService().SendCurrentAsync(request));
    }

    [Fact]
    public async Task SendGuestAsync_Delegates()
    {
        var request = GuestRequest();
        Guid expected = Guid.NewGuid();

        guestCheckout
            .Setup(service => service.SendAsync(request))
            .ReturnsAsync(expected);

        Assert.Equal(
            expected,
            await CreateService().SendGuestAsync(request));
    }

    private static OrderResponse Response() =>
        new()
        {
            Id = Guid.NewGuid(),
            OrderTotalPrice = 1m
        };

    private static SendOrderRequest SendRequest() =>
        new()
        {
            Names = "User",
            PostalCode = "7000",
            Country = "BG",
            City = "Ruse",
            Address = "Street",
            Phone = "1"
        };

    private static GuestOrderRequest GuestRequest() =>
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
                    ProductId = Guid.NewGuid(),
                    Quantity = 1
                }
            ]
        };
}
