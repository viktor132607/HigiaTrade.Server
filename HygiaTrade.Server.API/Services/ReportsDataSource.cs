using HygiaTrade.Core.Enums;
using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public sealed record ReportProductData(
    Guid Id,
    string Title,
    uint Quantity,
    string CategoryName);

public sealed record ReportOrderData(
    Guid Id,
    decimal OrderTotalPrice);

public sealed record ReportOrderItemData(
    Guid ProductId,
    string Title,
    int Quantity,
    decimal TotalPrice,
    Guid OrderId);

public interface IReportsDataSource
{
    Task<IReadOnlyList<ReportProductData>>
        GetProductsAsync(
            CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportOrderData>>
        GetOrdersAsync(
            DateTime fromUtc,
            DateTime toExclusiveUtc,
            CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportOrderItemData>>
        GetOrderItemsAsync(
            IReadOnlyCollection<Guid> orderIds,
            CancellationToken cancellationToken);
}

public sealed class ReportsDataSource(
    ApplicationDbContext db)
    : IReportsDataSource
{
    public async Task<IReadOnlyList<ReportProductData>>
        GetProductsAsync(
            CancellationToken cancellationToken) =>
        await db.Products
            .AsNoTracking()
            .Where(product => !product.IsDeleted)
            .Select(product => new ReportProductData(
                product.Id,
                product.Title,
                product.Quantity,
                product.Category != null
                    ? product.Category.Name
                    : "Без категория"))
            .OrderBy(product => product.Title)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReportOrderData>>
        GetOrdersAsync(
            DateTime fromUtc,
            DateTime toExclusiveUtc,
            CancellationToken cancellationToken) =>
        await db.Orders
            .AsNoTracking()
            .Where(order =>
                !order.IsDeleted &&
                order.Status != OrderStatus.AwaitingPayment &&
                order.Status != OrderStatus.Cancelled &&
                order.CreatedOn >= fromUtc &&
                order.CreatedOn < toExclusiveUtc)
            .Select(order => new ReportOrderData(
                order.Id,
                order.OrderTotalPrice))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReportOrderItemData>>
        GetOrderItemsAsync(
            IReadOnlyCollection<Guid> orderIds,
            CancellationToken cancellationToken)
    {
        Guid[] ids = orderIds.ToArray();

        return await db.OrderItems
            .AsNoTracking()
            .Where(item =>
                !item.IsDeleted &&
                ids.Contains(item.OrderId))
            .Select(item => new ReportOrderItemData(
                item.ProductId,
                item.Title,
                item.Quantity,
                item.TotalPrice,
                item.OrderId))
            .ToListAsync(cancellationToken);
    }
}
