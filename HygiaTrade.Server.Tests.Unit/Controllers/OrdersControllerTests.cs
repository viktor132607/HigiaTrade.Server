using HygiaTrade.API.Controllers;
using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Common.Requests.OrderItem;
using HygiaTrade.Common.Responses.Order;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Pages;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class OrdersControllerTests
{
    private readonly Mock<IOrderService> orderService = new();

    private OrdersController CreateController() =>
        new(orderService.Object);

    [Fact]
    public async Task GetAsync_ReturnsOk()
    {
        OrderResponse expected = CreateOrder();

        orderService
            .Setup(service => service.GetAsync())
            .ReturnsAsync(expected);

        IActionResult result = await CreateController().GetAsync();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task AddProductAsync_ReturnsOk()
    {
        AddOrderItemRequest request = new()
        {
            ProductId = Guid.NewGuid(),
            Quantity = 2
        };
        OrderResponse expected = CreateOrder();

        orderService
            .Setup(service => service.AddProductAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().AddProductAsync(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task RemoveProductAsync_ReturnsOk()
    {
        RemoveOrderItemRequest request = new()
        {
            ProductId = Guid.NewGuid(),
            Quantity = 1
        };
        OrderResponse expected = CreateOrder();

        orderService
            .Setup(service => service.RemoveProductAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().RemoveProductAsync(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task SendOrder_ReturnsOk_WhenServiceReturnsTrue()
    {
        SendOrderRequest request = CreateSendOrderRequest();

        orderService
            .Setup(service => service.SendCurrentAsync(request))
            .ReturnsAsync(true);

        IActionResult result =
            await CreateController().SendOrder(request);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task SendGuestOrder_ReturnsOk_WithOrderId()
    {
        GuestOrderRequest request = CreateGuestOrderRequest();
        Guid orderId = Guid.NewGuid();

        orderService
            .Setup(service => service.SendGuestAsync(request))
            .ReturnsAsync(orderId);

        IActionResult result =
            await CreateController().SendGuestOrder(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            orderId,
            ok.Value?.GetType().GetProperty("orderId")?.GetValue(ok.Value));
    }

    [Fact]
    public async Task SearchOrdersAsync_UsesProvidedRequest()
    {
        SearchOrderRequest request = new();
        Paginated<OrderResponse> expected = new()
        {
            Items = [CreateOrder()],
            TotalCount = 1
        };

        orderService
            .Setup(service => service.SearchOrdersAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().SearchOrdersAsync(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task SearchOrdersAsync_CreatesDefaultRequest_WhenNull()
    {
        orderService
            .Setup(service => service.SearchOrdersAsync(
                It.IsAny<SearchOrderRequest>()))
            .ReturnsAsync(new Paginated<OrderResponse>
            {
                Items = [CreateOrder()],
                TotalCount = 1
            });

        IActionResult result =
            await CreateController().SearchOrdersAsync(null);

        Assert.IsType<OkObjectResult>(result);

        orderService.Verify(
            service => service.SearchOrdersAsync(
                It.IsAny<SearchOrderRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_ReturnsOk_WhenServiceReturnsTrue()
    {
        ChangeOrderStatusRequest request = new()
        {
            OrderId = Guid.NewGuid(),
            OrderStatus = OrderStatus.Delivered
        };

        orderService
            .Setup(service => service.ChangeStatusAsync(request))
            .ReturnsAsync(true);

        IActionResult result =
            await CreateController().ChangeStatusAsync(request);

        Assert.IsType<OkResult>(result);
    }

    private static OrderResponse CreateOrder() => new()
    {
        Id = Guid.NewGuid(),
        OrderTotalPrice = 10m
    };

    private static SendOrderRequest CreateSendOrderRequest() => new()
    {
        Names = "Test User",
        PostalCode = "7000",
        Country = "BG",
        City = "Ruse",
        Address = "Test 1",
        Phone = "0888000000",
        ConsentAccepted = true
    };

    private static GuestOrderRequest CreateGuestOrderRequest() => new()
    {
        Names = "Guest",
        Email = "guest@example.com",
        PostalCode = "7000",
        Country = "BG",
        City = "Ruse",
        Address = "Test 1",
        Phone = "0888000000",
        ConsentAccepted = true,
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
