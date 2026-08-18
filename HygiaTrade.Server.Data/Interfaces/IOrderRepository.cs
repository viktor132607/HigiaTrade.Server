using HygiaTrade.Core.Enums;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
 Task<Order?> GetByUserIdAsync(Guid userId);   

 Task<Order?> GetByUserIdWithoutStatusRestrictionAsync(Guid userId);   

 Task<Order> AddAsync(Guid userId);

 Task<Order> ChangeStatusAsync(Guid orderId, OrderStatus newStatus);
}
