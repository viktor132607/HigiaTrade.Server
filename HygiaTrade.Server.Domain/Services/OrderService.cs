using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Common.Requests.OrderItem;
using HygiaTrade.Common.Responses.Order;
using HygiaTrade.Core.Pages;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public sealed class OrderService(
    IOrderAdministrationService administrationService,
    IOrderCartService cartService,
    ICurrentOrderCheckoutService currentCheckoutService,
    IGuestOrderCheckoutService guestCheckoutService) : IOrderService
{
    public Task<bool> ChangeStatusAsync(
        ChangeOrderStatusRequest request) =>
        administrationService.ChangeStatusAsync(request);

    public Task<Paginated<OrderResponse>> SearchOrdersAsync(
        SearchOrderRequest request) =>
        administrationService.SearchOrdersAsync(request);

    public Task<OrderResponse> GetAsync() =>
        cartService.GetAsync();

    public Task<OrderResponse> AddProductAsync(
        AddOrderItemRequest request) =>
        cartService.AddProductAsync(request);

    public Task<OrderResponse> RemoveProductAsync(
        RemoveOrderItemRequest request) =>
        cartService.RemoveProductAsync(request);

    public Task<bool> SendCurrentAsync(
        SendOrderRequest request) =>
        currentCheckoutService.SendAsync(request);

    public Task<Guid> SendGuestAsync(
        GuestOrderRequest request) =>
        guestCheckoutService.SendAsync(request);
}
