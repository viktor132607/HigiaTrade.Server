using HygiaTrade.Common.Requests.OrderItem;
using HygiaTrade.Common.Responses.Order;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public interface IOrderCartService
{
    Task<OrderResponse> GetAsync();

    Task<OrderResponse> AddProductAsync(
        AddOrderItemRequest request);

    Task<OrderResponse> RemoveProductAsync(
        RemoveOrderItemRequest request);
}

public sealed class OrderCartService(
    IAuthService authService,
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IOrderItemRepository orderItemRepository,
    IOrderPricingService pricingService)
    : IOrderCartService
{
    public async Task<OrderResponse> GetAsync()
    {
        Guid userId = await GetCurrentUserIdAsync();

        Order? order =
            await orderRepository.GetByUserIdAsync(userId);

        if (order is null)
        {
            throw new AppException("Order not found")
                .SetStatusCode(404);
        }

        return OrderMapping.ToResponse(order);
    }

    public async Task<OrderResponse> AddProductAsync(
        AddOrderItemRequest request)
    {
        Guid userId = await GetCurrentUserIdAsync();

        Order? order =
            await orderRepository.GetByUserIdAsync(userId);

        if (order is null)
        {
            order =
                await orderRepository.AddAsync(userId);
        }

        Product? product =
            await productRepository.GetByIdAsync(
                request.ProductId);

        if (product is null || !product.IsActive || product.IsDeleted)
        {
            throw new AppException("Product not found")
                .SetStatusCode(404);
        }

        OrderItem? existingItem = order.Items
            .FirstOrDefault(item =>
                item.ProductId == request.ProductId);

        int nextQuantity =
            (existingItem?.Quantity ?? 0) +
            request.Quantity;

        if (nextQuantity <= 0)
        {
            throw new AppException(
                    "Quantity must be greater than zero.")
                .SetStatusCode(400);
        }

        if (product.Quantity < nextQuantity)
        {
            throw new AppException(
                    "Insufficient stock for the selected quantity.")
                .SetStatusCode(409);
        }

        if (existingItem is not null)
        {
            pricingService.ApplyCurrentPricing(
                existingItem,
                product,
                nextQuantity);
        }
        else
        {
            var newOrderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                SinglePrice = 0m,
                TotalPrice = 0m,
                PrimaryImageUri = product.MainImageUrl,
                Title = product.Title,
            };

            pricingService.ApplyCurrentPricing(
                newOrderItem,
                product,
                request.Quantity);

            await orderItemRepository.AddAsync(
                newOrderItem);

            order.Items.Add(newOrderItem);
        }

        pricingService.UpdateOrderPrices(order);

        await orderRepository.UpdateAsync(order);

        return OrderMapping.ToResponse(order);
    }

    public async Task<OrderResponse> RemoveProductAsync(
        RemoveOrderItemRequest request)
    {
        Guid userId = await GetCurrentUserIdAsync();

        Order? order =
            await orderRepository.GetByUserIdAsync(userId);

        if (order is null)
        {
            throw new AppException("Order not found")
                .SetStatusCode(404);
        }

        OrderItem? item = order.Items
            .FirstOrDefault(orderItem =>
                orderItem.ProductId ==
                request.ProductId);

        if (item is null)
        {
            throw new AppException("Product not found")
                .SetStatusCode(404);
        }

        item.Quantity -= request.Quantity;

        if (item.Quantity <= 0)
        {
            order.Items.Remove(item);

            if (order.Items.Count == 0)
            {
                await orderRepository.DeleteAsync(order.Id);

                throw new AppException("Order deleted")
                    .SetStatusCode(200);
            }
        }
        else
        {
            Product? product =
                await productRepository.GetByIdAsync(
                    item.ProductId);

            if (product is null || !product.IsActive || product.IsDeleted)
            {
                throw new AppException("Product not found")
                    .SetStatusCode(404);
            }

            pricingService.ApplyCurrentPricing(
                item,
                product,
                item.Quantity);
        }

        pricingService.UpdateOrderPrices(order);

        await orderRepository.UpdateAsync(order);

        return OrderMapping.ToResponse(order);
    }

    private async Task<Guid> GetCurrentUserIdAsync() =>
        Guid.Parse(
            (await authService.GetCurrentUserId())!);
}
