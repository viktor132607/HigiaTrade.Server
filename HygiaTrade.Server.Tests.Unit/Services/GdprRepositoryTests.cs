using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class GdprRepositoryTests
{
    [Fact]
    public async Task GetExportDataAsync_FiltersDeletedRowsAndOrdersDescending()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        User user = UserFor("active@example.com");
        User otherUser = UserFor("other@example.com");

        Order older = new()
        {
            UserId = user.Id,
            User = user,
            Names = "Older",
            CreatedOn =
                new DateTime(
                    2026,
                    9,
                    10,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc)
        };

        Order newer = new()
        {
            UserId = user.Id,
            User = user,
            Names = "Newer",
            CreatedOn =
                new DateTime(
                    2026,
                    9,
                    20,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc)
        };

        Order deletedOrder = new()
        {
            UserId = user.Id,
            User = user,
            IsDeleted = true
        };

        Order otherOrder = new()
        {
            UserId = otherUser.Id,
            User = otherUser
        };

        OrderItem item = new()
        {
            OrderId = newer.Id,
            Order = newer,
            ProductId = Guid.NewGuid(),
            Product = null!,
            Quantity = 1,
            SinglePrice = 5m,
            TotalPrice = 5m,
            Title = "Product",
            PrimaryImageUri = "image"
        };

        newer.Items.Add(item);

        WishlistItem wishlist = WishlistFor(user, false);
        WishlistItem deletedWishlist = WishlistFor(user, true);
        WishlistItem otherWishlist = WishlistFor(otherUser, false);

        Review olderReview = ReviewFor(
            user,
            false,
            new DateTime(
                2026,
                9,
                11,
                0,
                0,
                0,
                DateTimeKind.Utc));

        Review newerReview = ReviewFor(
            user,
            false,
            new DateTime(
                2026,
                9,
                21,
                0,
                0,
                0,
                DateTimeKind.Utc));

        Review deletedReview = ReviewFor(
            user,
            true,
            DateTime.UtcNow);

        db.AddRange(
            user,
            otherUser,
            older,
            newer,
            deletedOrder,
            otherOrder,
            wishlist,
            deletedWishlist,
            otherWishlist,
            olderReview,
            newerReview,
            deletedReview);

        await db.SaveChangesAsync();

        var repository =
            new GdprRepository(db);

        GdprExportData? result =
            await repository.GetExportDataAsync(
                user.Id);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.User.Id);

        Assert.Equal(
            [newer.Id, older.Id],
            result.Orders.Select(x => x.Id));

        Assert.Single(result.Orders[0].Items);
        Assert.Single(result.WishlistItems);
        Assert.Equal(wishlist.Id, result.WishlistItems[0].Id);

        Assert.Equal(
            [newerReview.Id, olderReview.Id],
            result.Reviews.Select(x => x.Id));
    }

    [Fact]
    public async Task GetExportDataAsync_ReturnsNullForDeletedOrMissingUser()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        User deleted = UserFor("deleted@example.com");
        deleted.IsDeleted = true;

        db.Add(deleted);
        await db.SaveChangesAsync();

        var repository =
            new GdprRepository(db);

        Assert.Null(
            await repository.GetExportDataAsync(
                deleted.Id));

        Assert.Null(
            await repository.GetExportDataAsync(
                Guid.NewGuid()));
    }

    [Fact]
    public async Task GetDeletionDataAsync_ReturnsTrackedNonDeletedPersonalRows()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        User user = UserFor("active@example.com");

        Order order = new()
        {
            UserId = user.Id,
            User = user
        };

        Order deletedOrder = new()
        {
            UserId = user.Id,
            User = user,
            IsDeleted = true
        };

        WishlistItem wishlist = WishlistFor(user, false);
        WishlistItem deletedWishlist = WishlistFor(user, true);
        Review review = ReviewFor(user, false, DateTime.UtcNow);
        Review deletedReview = ReviewFor(user, true, DateTime.UtcNow);

        db.AddRange(
            user,
            order,
            deletedOrder,
            wishlist,
            deletedWishlist,
            review,
            deletedReview);

        await db.SaveChangesAsync();

        var repository =
            new GdprRepository(db);

        GdprDeletionData? result =
            await repository.GetDeletionDataAsync(
                user.Id);

        Assert.NotNull(result);
        Assert.Same(user, result.User);
        Assert.Single(result.Orders);
        Assert.Single(result.WishlistItems);
        Assert.Single(result.Reviews);

        result.User.Names = "Changed";
        await repository.SaveChangesAsync();

        Assert.Equal(
            "Changed",
            (await db.Users.FindAsync(user.Id))!.Names);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"gdpr-{Guid.NewGuid():N}")
                .Options;

        return new ApplicationDbContext(options);
    }

    private static User UserFor(string email) =>
        new()
        {
            Email = email,
            Names = "User",
            Phone = "123",
            PasswordHash = "hash"
        };

    private static WishlistItem WishlistFor(
        User user,
        bool isDeleted) =>
        new()
        {
            UserId = user.Id,
            User = user,
            ProductId = Guid.NewGuid(),
            Product = null!,
            IsDeleted = isDeleted
        };

    private static Review ReviewFor(
        User user,
        bool isDeleted,
        DateTime createdOn) =>
        new()
        {
            UserId = user.Id,
            User = user,
            ProductId = Guid.NewGuid(),
            Product = null!,
            Content = "Review",
            Rating = 5,
            CreatedOn = createdOn,
            IsDeleted = isDeleted
        };
}
