using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public interface ICurrentOrderCheckoutService
{
    Task<bool> SendAsync(SendOrderRequest request);
}

public sealed class CurrentOrderCheckoutService(
    IAuthService authService,
    IOrderRepository orderRepository,
    IUserRepository userRepository,
    IEmailNotificationService emailNotificationService,
    IOrderPricingService pricingService,
    IOrderStockService stockService,
    IOrderPaymentMethodResolver paymentMethodResolver)
    : ICurrentOrderCheckoutService
{
    public async Task<bool> SendAsync(
        SendOrderRequest request)
    {
        Guid userId = Guid.Parse(
            (await authService.GetCurrentUserId())!);

        Order? order =
            await orderRepository.GetByUserIdAsync(userId);

        if (order is null || !order.Items.Any())
        {
            throw new AppException("Order not found")
                .SetStatusCode(404);
        }

        if (!request.ConsentAccepted)
        {
            throw new AppException(
                    "Consent is required to place an order.")
                .SetStatusCode(400);
        }

        string paymentMethod =
            paymentMethodResolver.Resolve(
                request.PaymentMethod);

        string deliveryMethod =
            string.IsNullOrWhiteSpace(
                request.DeliveryMethod)
                ? "standard-courier"
                : request.DeliveryMethod.Trim();

        await pricingService
            .RefreshCurrentCartPricingAsync(order);

        await stockService
            .EnsureAvailabilityAsync(order);

        order.Names = request.Names;
        order.PostalCode = request.PostalCode;
        order.Country = request.Country;
        order.City = request.City;
        order.Address = request.Address;
        order.Phone = request.Phone;
        order.Status =
            OrderStatus.PendingVerification;

        await orderRepository.UpdateAsync(order);

        await stockService
            .DecreaseQuantitiesAsync(order);

        User? user =
            await userRepository.GetByIdAsync(userId);

        if (user is not null)
        {
            await emailNotificationService
                .SendOrderConfirmationAsync(
                    user,
                    order,
                    paymentMethod,
                    deliveryMethod);
        }

        return true;
    }
}
