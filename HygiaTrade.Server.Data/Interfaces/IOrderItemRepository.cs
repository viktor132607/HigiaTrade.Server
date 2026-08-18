using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Interfaces;

public interface IOrderItemRepository : IRepository<OrderItem>
{
    Task<bool> AddRange(ICollection<OrderItem> orderItems);
}
