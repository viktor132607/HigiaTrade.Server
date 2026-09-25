using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Common.Responses.Order;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Core.Pages;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Data.PaginationAndFiltering;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public interface IOrderAdministrationService
{
    Task<bool> ChangeStatusAsync(
        ChangeOrderStatusRequest request);

    Task<Paginated<OrderResponse>> SearchOrdersAsync(
        SearchOrderRequest request);
}

public sealed class OrderAdministrationService(
    IOrderRepository orderRepository,
    IAuthService authService,
    IUserRepository userRepository,
    IEmailNotificationService emailNotificationService,
    IOrderStockService stockService)
    : IOrderAdministrationService
{
    public async Task<bool> ChangeStatusAsync(
        ChangeOrderStatusRequest request)
    {
        Order? order =
            await orderRepository.GetByIdAsync(request.OrderId);

        if (order is null)
        {
            throw new AppException("Order not found")
                .SetStatusCode(404);
        }

        if (request.OrderStatus == OrderStatus.AwaitingPayment || (order.PaymentMethod == "online-card" && request.OrderStatus == OrderStatus.Created))
            throw new AppException("Payment state is managed by Stripe.").SetStatusCode(409);
        if (order.PaymentMethod == "online-card" && order.PaymentStatus != "Paid")
            throw new AppException("Card payment must be confirmed by Stripe. Cancel unpaid payments through the payment page.").SetStatusCode(409);
        if (order.PaymentMethod == "online-card" && request.OrderStatus == OrderStatus.Cancelled)
            throw new AppException("Paid card orders require a refund through Stripe before cancellation.").SetStatusCode(409);

        if (request.OrderStatus != OrderStatus.Cancelled &&
            order.Status == OrderStatus.Created)
        {
            await stockService.EnsureAvailabilityAsync(order);
            await stockService.DecreaseQuantitiesAsync(order);
        }

        await orderRepository.ChangeStatusAsync(
            request.OrderId,
            request.OrderStatus);

        User? user =
            await userRepository.GetByIdAsync(order.UserId);

        if (user is not null)
        {
            await emailNotificationService
                .SendOrderStatusChangedAsync(user, order);
        }

        return true;
    }

    public async Task<Paginated<OrderResponse>> SearchOrdersAsync(
        SearchOrderRequest request)
    {
        if (request.UserId is null)
        {
            string? role =
                await authService.GetCurrentUserRole();

            if (role != Roles.Admin)
            {
                throw new AppException("Forbidden")
                    .SetStatusCode(403);
            }
        }

        var filter = new Filter<Order>
        {
            Includes =
            [
                order => order.Items
            ],
            Predicate = request.GetPredicate(),
            PageNumber = request.PageNumber ?? 1,
            PageSize = request.PageSize ?? 10,
            SortBy = request.SortBy ?? "CreatedOn",
            SortDescending =
                request.SortDescending ?? false,
        };

        Paginated<Order> result =
            await orderRepository.SearchAsync(filter);

        List<OrderResponse> responses = [];

        foreach (Order order in result.Items!)
        {
            responses.Add(OrderMapping.ToResponse(order));
        }

        return new Paginated<OrderResponse>
        {
            Items = responses,
            TotalCount = result.TotalCount
        };
    }
}
