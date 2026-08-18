using System.Reflection;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;

namespace HygiaTrade.Data.Repositories;

public class OrderRepository(ApplicationDbContext context) : Repository<Order>(context), IOrderRepository
{
    public async Task<Order?> GetByUserIdAsync(Guid userId)
    {
        return await context.Orders
            .Include(x => x.User)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == OrderStatus.Created && !x.IsDeleted);
    }

    public async Task<Order?> GetByUserIdWithoutStatusRestrictionAsync(Guid userId)
    {
        return await context.Orders
            .Include(x => x.User)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);    }

    public async Task<Order> AddAsync(Guid userId)
    {
        if (context.Users.Any(u => u.Id == userId && u.IsDeleted == false) == false)
        {
            throw new AppException("User not found").SetStatusCode(404);   
        }
        
        Order newOrder = new()
        {
            UserId = userId,
        };
        
        await context.Orders.AddAsync(newOrder);
        await context.SaveChangesAsync();
        
        return newOrder;
    }

    public async Task<Order> ChangeStatusAsync(Guid orderId, OrderStatus newStatus)
    {
        Order? dbOrder = await context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

        if (dbOrder == null)
        {
            throw new AppException("Order not found").SetStatusCode(404);
        }

        dbOrder.Status = newStatus;
        await context.SaveChangesAsync();
        return dbOrder;
    }
    
    public override async ValueTask<Order?> UpdateAsync(Order order)
    {
        Order? existingOrder = await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        if (existingOrder == null)
        {
            return null;
        }

        context.Entry(existingOrder).CurrentValues.SetValues(order);

        PropertyInfo? modifiedOnProperty = typeof(Order).GetProperty("ModifiedOn");
        if (modifiedOnProperty != null && modifiedOnProperty.PropertyType == typeof(DateTime))
        {
            modifiedOnProperty.SetValue(existingOrder, DateTime.UtcNow);
        }

        foreach (OrderItem item in order.Items)
        {
            OrderItem? existingItem = existingOrder.Items.FirstOrDefault(x => x.Id == item.Id);

            if (existingItem != null)
            {
                context.Entry(existingItem).CurrentValues.SetValues(item);

                PropertyInfo? itemModifiedOn = typeof(OrderItem).GetProperty("ModifiedOn");
                if (itemModifiedOn != null && itemModifiedOn.PropertyType == typeof(DateTime))
                {
                    itemModifiedOn.SetValue(existingItem, DateTime.UtcNow);
                }
            }
            else
            {
                if (item.Id == Guid.Empty)
                {
                    item.OrderId = existingOrder.Id;

                    PropertyInfo? itemModifiedOn = typeof(OrderItem).GetProperty("ModifiedOn");
                    if (itemModifiedOn != null && itemModifiedOn.PropertyType == typeof(DateTime))
                    {
                        itemModifiedOn.SetValue(item, DateTime.UtcNow);
                    }

                    context.Entry(item).State = EntityState.Added;
                }
            }
        }

        foreach (OrderItem existingItem in existingOrder.Items.ToList())
        {
            if (!order.Items.Any(i => i.Id == existingItem.Id))
            {
                context.Remove(existingItem);
            }
        }

        await context.SaveChangesAsync();
        return existingOrder;
    }
}
