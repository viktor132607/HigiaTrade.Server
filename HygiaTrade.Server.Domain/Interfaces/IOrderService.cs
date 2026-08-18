using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Common.Requests.OrderItem;
using HygiaTrade.Common.Responses.Order;
using HygiaTrade.Core.Pages;

namespace HygiaTrade.Domain.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> GetAsync();
    Task<OrderResponse> AddProductAsync(AddOrderItemRequest product);
    Task<OrderResponse> RemoveProductAsync(RemoveOrderItemRequest product);
    Task<bool> SendCurrentAsync(SendOrderRequest request);
    Task<bool> ChangeStatusAsync(ChangeOrderStatusRequest request);
    Task<Paginated<OrderResponse>> SearchOrdersAsync(SearchOrderRequest request);
}
 
