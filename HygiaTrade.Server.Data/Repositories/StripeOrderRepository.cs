using HygiaTrade.Core.Enums;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Data.Repositories;

public sealed class StripeOrderRepository(ApplicationDbContext context) : IStripeOrderRepository
{
    public async Task<T> LockedAsync<T>(Guid key, Func<Task<T>> action)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        // Serializes retries and payment finalization across API instances, not just threads.
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key.ToString()}, 0))");
        context.ChangeTracker.Clear();
        var result = await action();
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    public Task<Order?> FindAttemptAsync(Guid id) => context.Orders.Include(o => o.Items)
        .SingleOrDefaultAsync(o => o.StripeCheckoutAttemptId == id && !o.IsDeleted);
    public Task<Order?> FindSessionAsync(string id) => context.Orders.Include(o => o.Items)
        .SingleOrDefaultAsync(o => o.StripeSessionId == id && !o.IsDeleted);
    public Task<Product?> LockProductAsync(Guid id) => context.Products
        .FromSqlInterpolated($"SELECT * FROM \"Products\" WHERE \"Id\" = {id} FOR UPDATE")
        .SingleOrDefaultAsync();
    public async Task AddAsync(Order order) { context.Orders.Add(order); await context.SaveChangesAsync(); }
    public Task SaveAsync() => context.SaveChangesAsync();
    public Task<List<Order>> PendingAsync() => context.Orders.AsNoTracking().Include(o => o.Items)
        .Where(o => o.PaymentMethod == "online-card" && o.PaymentStatus == "Pending" && !o.IsDeleted)
        .OrderBy(o => o.ModifiedOn).Take(100).ToListAsync();

    public async Task FinalizeAsync(Order order, bool paid)
    {
        if (order.PaymentStatus != "Pending") return;
        if (!paid)
        {
            foreach (var item in order.Items.OrderBy(i => i.ProductId))
            {
                var product = await LockProductAsync(item.ProductId);
                if (product is not null) product.Quantity += (uint)item.Quantity;
            }
            order.PaymentStatus = "Expired";
            order.Status = OrderStatus.Cancelled;
        }
        else
        {
            order.PaymentStatus = "Paid";
            order.Status = OrderStatus.PendingVerification;
            // Remove only the purchased quantities from the account's draft cart.
            if (order.UserId is Guid userId)
            {
                var draft = await context.Orders.Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.UserId == userId && o.Status == OrderStatus.Created && !o.IsDeleted);
                if (draft is not null)
                {
                    foreach (var purchased in order.Items)
                    {
                        var item = draft.Items.FirstOrDefault(i => i.ProductId == purchased.ProductId);
                        if (item is null) continue;
                        if (item.Quantity <= purchased.Quantity) { draft.Items.Remove(item); context.OrderItems.Remove(item); }
                        else
                        {
                            var ratio = (decimal)(item.Quantity - purchased.Quantity) / item.Quantity;
                            item.Quantity -= purchased.Quantity;
                            item.TotalPrice = decimal.Round(item.SinglePrice * item.Quantity, 2, MidpointRounding.AwayFromZero);
                            item.TotalPriceExclVat = decimal.Round(item.TotalPriceExclVat * ratio, 2, MidpointRounding.AwayFromZero);
                            item.VatAmount = item.TotalPrice - item.TotalPriceExclVat;
                        }
                    }
                    draft.OrderTotalPrice = draft.Items.Sum(i => i.TotalPrice);
                    draft.OrderSubtotalExclVat = draft.Items.Sum(i => i.TotalPriceExclVat);
                    draft.OrderVatAmount = draft.Items.Sum(i => i.VatAmount);
                }
            }
        }
        order.ModifiedOn = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }
}
