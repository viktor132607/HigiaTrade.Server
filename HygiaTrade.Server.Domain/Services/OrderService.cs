using Microsoft.Extensions.Options;
using HygiaTrade.Common.Options;
using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Common.Requests.OrderItem;
using HygiaTrade.Common.Responses.Order;
using HygiaTrade.Common.Responses.OrderItem;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Core.Pages;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Data.PaginationAndFiltering;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public class OrderService(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IAuthService authService,
    IOrderItemRepository orderItemRepository,
    IUserRepository userRepository,
    IEmailNotificationService emailNotificationService,
    IOptions<PaymentOptions> paymentOptions) : IOrderService
{
    private readonly PaymentOptions _paymentOptions = paymentOptions.Value;

    public async Task<bool> ChangeStatusAsync(ChangeOrderStatusRequest request)
    {
        Order? order = await orderRepository.GetByIdAsync(request.OrderId);

        if (order == null)
        {
            throw new AppException("Order not found").SetStatusCode(404);
        }

        if (request.OrderStatus != OrderStatus.Cancelled && order.Status == OrderStatus.Created)
        {
            await EnsureStockAvailabilityAsync(order);
            await DecreaseProductQuantitiesAsync(order);
        }

        await orderRepository.ChangeStatusAsync(request.OrderId, request.OrderStatus);

        User? user = await userRepository.GetByIdAsync(order.UserId);
        if (user != null)
        {
            await emailNotificationService.SendOrderStatusChangedAsync(user, order);
        }

        return true;
    }

    public async Task<Paginated<OrderResponse>> SearchOrdersAsync(SearchOrderRequest request)
    {
        if (request.UserId == null)
        {
            string? role = await authService.GetCurrentUserRole();
            if (role != Roles.Admin)
            {
                throw new AppException("Forbidden").SetStatusCode(403);
            }
        }

        Filter<Order> filter = new()
        {
            Includes =
            [
                x => x.Items
            ],
            Predicate = request.GetPredicate(),
            PageNumber = request.PageNumber ?? 1,
            PageSize = request.PageSize ?? 10,
            SortBy = request.SortBy ?? "CreatedOn",
            SortDescending = request.SortDescending ?? false,
        };

        Paginated<Order> result = await orderRepository.SearchAsync(filter);

        List<OrderResponse> responses = new();

        foreach (Order order in result.Items!)
        {
            responses.Add(MapOrderToResponse(order));
        }

        return new Paginated<OrderResponse>
        {
            Items = responses,
            TotalCount = result.TotalCount
        };
    }

    public async Task<OrderResponse> GetAsync()
    {
        Guid userId = Guid.Parse((await authService.GetCurrentUserId())!);
        Order? order = await orderRepository.GetByUserIdAsync(userId);

        if (order == null)
        {
            throw new AppException("Order not found").SetStatusCode(404);
        }

        return MapOrderToResponse(order);
    }

    public async Task<OrderResponse> AddProductAsync(AddOrderItemRequest request)
    {
        Guid userId = Guid.Parse((await authService.GetCurrentUserId())!);
        Order? order = await orderRepository.GetByUserIdAsync(userId);

        if (order == null)
        {
            order = await orderRepository.AddAsync(userId);
        }

        Product? product = await productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            throw new AppException("Product not found").SetStatusCode(404);
        }

        OrderItem? existingItem = order.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        int nextQuantity = (existingItem?.Quantity ?? 0) + request.Quantity;
        if (product.Quantity < nextQuantity)
        {
            throw new AppException("Insufficient stock for the selected quantity.").SetStatusCode(409);
        }

        if (existingItem != null)
        {
            existingItem.Quantity += request.Quantity;
            existingItem.TotalPrice = existingItem.Quantity * existingItem.SinglePrice;
        }
        else
        {
            decimal singlePrice = product.DiscountedPrice != 0m
                ? product.DiscountedPrice
                : product.RegularPrice;

            OrderItem newOrderItem = new()
            {
                OrderId = order.Id,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                SinglePrice = singlePrice,
                TotalPrice = request.Quantity * singlePrice,
                PrimaryImageUri = product.MainImageUrl,
                Title = product.Title,
            };

            await orderItemRepository.AddAsync(newOrderItem);
            order.Items.Add(newOrderItem);
        }

        UpdateOrderPrices(order);

        await orderRepository.UpdateAsync(order);

        return MapOrderToResponse(order);
    }

    public async Task<OrderResponse> RemoveProductAsync(RemoveOrderItemRequest request)
    {
        Guid userId = Guid.Parse((await authService.GetCurrentUserId())!);
        Order? order = await orderRepository.GetByUserIdAsync(userId);

        if (order == null)
        {
            throw new AppException("Order not found").SetStatusCode(404);
        }

        OrderItem? item = order.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
        if (item == null)
        {
            throw new AppException("Product not found").SetStatusCode(404);
        }

        item.Quantity -= request.Quantity;

        if (item.Quantity <= 0)
        {
            order.Items.Remove(item);
            if (order.Items.Count == 0)
            {
                await orderRepository.DeleteAsync(order.Id);
                throw new AppException("Order deleted").SetStatusCode(200);
            }
        }
        else
        {
            item.TotalPrice = item.Quantity * item.SinglePrice;
        }

        UpdateOrderPrices(order);

        await orderRepository.UpdateAsync(order);

        return MapOrderToResponse(order);
    }

    public async Task<bool> SendCurrentAsync(SendOrderRequest request)
    {
        Guid userId = Guid.Parse((await authService.GetCurrentUserId())!);
        Order? order = await orderRepository.GetByUserIdAsync(userId);

        if (order == null || !order.Items.Any())
        {
            throw new AppException("Order not found").SetStatusCode(404);
        }

        if (!request.ConsentAccepted)
        {
            throw new AppException("Consent is required to place an order.").SetStatusCode(400);
        }

        string paymentMethod = ResolvePaymentMethod(request.PaymentMethod);
        string deliveryMethod = string.IsNullOrWhiteSpace(request.DeliveryMethod)
            ? "standard-courier"
            : request.DeliveryMethod.Trim();

        await EnsureStockAvailabilityAsync(order);

        order.Names = request.Names;
        order.PostalCode = request.PostalCode;
        order.Country = request.Country;
        order.City = request.City;
        order.Address = request.Address;
        order.Phone = request.Phone;
        order.Status = OrderStatus.PendingVerification;

        await orderRepository.UpdateAsync(order);
        await DecreaseProductQuantitiesAsync(order);

        User? user = await userRepository.GetByIdAsync(userId);
        if (user != null)
        {
            await emailNotificationService.SendOrderConfirmationAsync(user, order, paymentMethod, deliveryMethod);
        }

        return true;
    }

    private void UpdateOrderPrices(Order order)
    {
        order.OrderTotalPrice = order.Items.Sum(i => i.TotalPrice);
    }

    private OrderResponse MapOrderToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderTotalPrice = order.OrderTotalPrice,
            Names = order.Names,
            PostalCode = order.PostalCode,
            Country = order.Country,
            City = order.City,
            Address = order.Address,
            Phone = order.Phone,
            Status = order.Status,
            CreatedOn = order.CreatedOn,
            Items = order.Items.Select(i => new OrderItemResponse
                {
                    ProductId = i.ProductId,
                    SinglePrice = i.SinglePrice,
                    TotalPrice = i.TotalPrice,
                    Quantity = i.Quantity,
                    PrimaryImageUri = i.PrimaryImageUri,
                    Title = i.Title
                })
                .OrderBy(i => i.Title)
                .ToList()
        };
    }

    private async Task EnsureStockAvailabilityAsync(Order order)
    {
        foreach (OrderItem item in order.Items)
        {
            Product? product = await productRepository.GetByIdAsync(item.ProductId);
            if (product == null || product.Quantity < item.Quantity)
            {
                throw new AppException($"Product '{item.Title}' is out of stock or has insufficient quantity.").SetStatusCode(409);
            }
        }
    }

    private async Task DecreaseProductQuantitiesAsync(Order order)
    {
        foreach (OrderItem item in order.Items)
        {
            Product? product = await productRepository.GetByIdAsync(item.ProductId);
            if (product == null)
            {
                throw new AppException("Product not found").SetStatusCode(404);
            }

            if (product.Quantity < item.Quantity)
            {
                throw new AppException($"Product '{item.Title}' is out of stock or has insufficient quantity.").SetStatusCode(409);
            }

            product.Quantity -= (uint)item.Quantity;

            await productRepository.UpdateAsync(product);
        }
    }

    private string ResolvePaymentMethod(string? paymentMethod)
    {
        string candidate = string.IsNullOrWhiteSpace(paymentMethod)
            ? _paymentOptions.SupportedMethods.FirstOrDefault() ?? "online-card"
            : paymentMethod.Trim();

        if (!_paymentOptions.SupportedMethods.Contains(candidate, StringComparer.OrdinalIgnoreCase))
        {
            throw new AppException("Unsupported payment method.").SetStatusCode(400);
        }

        return candidate;
    }
}
