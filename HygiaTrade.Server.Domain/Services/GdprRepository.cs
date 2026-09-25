using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Domain.Services;

public sealed record GdprExportData(
    User User,
    IReadOnlyList<Order> Orders,
    IReadOnlyList<WishlistItem> WishlistItems,
    IReadOnlyList<Review> Reviews);

public sealed record GdprDeletionData(
    User User,
    IReadOnlyList<Order> Orders,
    IReadOnlyList<WishlistItem> WishlistItems,
    IReadOnlyList<Review> Reviews);

public interface IGdprRepository
{
    Task<GdprExportData?> GetExportDataAsync(Guid userId);

    Task<GdprDeletionData?> GetDeletionDataAsync(Guid userId);

    Task SaveChangesAsync();
}

public sealed class GdprRepository(
    ApplicationDbContext context) : IGdprRepository
{
    public async Task<GdprExportData?> GetExportDataAsync(
        Guid userId)
    {
        User? user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Id == userId &&
                !user.IsDeleted);

        if (user is null)
        {
            return null;
        }

        List<Order> orders = await context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .Where(order =>
                order.UserId == userId &&
                !order.IsDeleted)
            .OrderByDescending(order =>
                order.CreatedOn)
            .ToListAsync();

        List<WishlistItem> wishlistItems =
            await context.WishlistItems
                .AsNoTracking()
                .Where(item =>
                    item.UserId == userId &&
                    !item.IsDeleted)
                .ToListAsync();

        List<Review> reviews = await context.Reviews
            .AsNoTracking()
            .Where(review =>
                review.UserId == userId &&
                !review.IsDeleted)
            .OrderByDescending(review =>
                review.CreatedOn)
            .ToListAsync();

        return new GdprExportData(
            user,
            orders,
            wishlistItems,
            reviews);
    }

    public async Task<GdprDeletionData?> GetDeletionDataAsync(
        Guid userId)
    {
        User? user = await context.Users
            .FirstOrDefaultAsync(user =>
                user.Id == userId &&
                !user.IsDeleted);

        if (user is null)
        {
            return null;
        }

        List<WishlistItem> wishlistItems =
            await context.WishlistItems
                .Where(item =>
                    item.UserId == userId &&
                    !item.IsDeleted)
                .ToListAsync();

        List<Review> reviews = await context.Reviews
            .Where(review =>
                review.UserId == userId &&
                !review.IsDeleted)
            .ToListAsync();

        List<Order> orders = await context.Orders
            .Where(order =>
                order.UserId == userId &&
                !order.IsDeleted)
            .ToListAsync();

        return new GdprDeletionData(
            user,
            orders,
            wishlistItems,
            reviews);
    }

    public async Task SaveChangesAsync() =>
        await context.SaveChangesAsync();
}
