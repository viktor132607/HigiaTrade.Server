using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;

namespace HygiaTrade.Domain.Services;

public interface IGuestOrderCheckoutService
{
    Task<Guid> SendAsync(GuestOrderRequest request);
}

public sealed class GuestOrderCheckoutService(
    IGuestOrderRepository guestOrderRepository,
    IOrderPricingService pricingService)
    : IGuestOrderCheckoutService
{
    public async Task<Guid> SendAsync(
        GuestOrderRequest request)
    {
        if (!request.ConsentAccepted)
        {
            throw new AppException(
                    "Consent is required to place an order.")
                .SetStatusCode(400);
        }

        if (request.Items.Count == 0)
        {
            throw new AppException("Cart is empty.")
                .SetStatusCode(400);
        }

        CheckoutPolicy.Validate(request);

        List<Guid> productIds = request.Items
            .Select(item => item.ProductId)
            .Distinct()
            .ToList();

        Dictionary<Guid, Product> products =
            await guestOrderRepository
                .GetAvailableProductsAsync(productIds);

        if (products.Count != productIds.Count)
        {
            throw new AppException(
                    "One or more products are no longer available.")
                .SetStatusCode(409);
        }

        var order = new Order
        {
            GuestEmail = request.Email.Trim(),
            Names = request.Names.Trim(),
            PostalCode = request.PostalCode.Trim(),
            Country = request.Country.Trim(),
            City = request.City.Trim(),
            Address = request.Address.Trim(),
            Phone = request.Phone.Trim(),
            Status = OrderStatus.PendingVerification
        };

        CheckoutPolicy.Apply(order, request);

        foreach (GuestOrderItemRequest requestedItem
                 in request.Items)
        {
            Product product =
                products[requestedItem.ProductId];

            if (requestedItem.Quantity < 1 ||
                product.Quantity <
                requestedItem.Quantity)
            {
                throw new AppException(
                        $"Insufficient stock for '{product.Title}'.")
                    .SetStatusCode(409);
            }

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = requestedItem.Quantity,
                SinglePrice = 0m,
                TotalPrice = 0m,
                Title = product.Title,
                PrimaryImageUri = product.MainImageUrl
            };

            pricingService.ApplyCurrentPricing(
                orderItem,
                product,
                requestedItem.Quantity);

            order.Items.Add(orderItem);

            product.Quantity -=
                (uint)requestedItem.Quantity;
        }

        pricingService.UpdateOrderPrices(order);
        CheckoutPolicy.EnsureMinimum(order.OrderTotalPrice);

        return await guestOrderRepository.SaveAsync(
            order,
            products.Values.ToArray());
    }
}
