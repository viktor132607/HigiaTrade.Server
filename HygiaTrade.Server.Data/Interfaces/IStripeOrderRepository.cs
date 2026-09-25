using HygiaTrade.Data.Entities;
namespace HygiaTrade.Data.Interfaces;

public interface IStripeOrderRepository
{
    Task<T> LockedAsync<T>(Guid key, Func<Task<T>> action);
    Task<Order?> FindAttemptAsync(Guid attemptId);
    Task<Order?> FindSessionAsync(string sessionId);
    Task<Product?> LockProductAsync(Guid productId);
    Task AddAsync(Order order);
    Task SaveAsync();
    Task FinalizeAsync(Order order, bool paid);
    Task<List<Order>> PendingAsync();
}
