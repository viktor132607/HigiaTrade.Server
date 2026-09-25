using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class GdprDataAnonymizerTests
{
    [Fact]
    public void Anonymize_SoftDeletesPersonalCollectionsAndScrubsUserAndOrders()
    {
        User user = new()
        {
            Email = "user@example.com",
            Names = "User",
            Phone = "123",
            PasswordHash = "hash",
            RefreshToken = "token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };

        WishlistItem wishlist = new()
        {
            UserId = user.Id,
            User = user,
            ProductId = Guid.NewGuid(),
            Product = null!
        };

        Review review = new()
        {
            UserId = user.Id,
            User = user,
            ProductId = Guid.NewGuid(),
            Product = null!,
            Content = "Review",
            Rating = 4
        };

        Order order = new()
        {
            UserId = user.Id,
            Names = "User",
            PostalCode = "9000",
            Country = "BG",
            City = "Varna",
            Address = "Address",
            Phone = "456"
        };

        var data = new GdprDeletionData(
            user,
            [order],
            [wishlist],
            [review]);

        new GdprDataAnonymizer()
            .Anonymize(data);

        Assert.True(wishlist.IsDeleted);
        Assert.True(review.IsDeleted);

        Assert.Equal("Deleted user", order.Names);
        Assert.Null(order.PostalCode);
        Assert.Null(order.Country);
        Assert.Null(order.City);
        Assert.Null(order.Address);
        Assert.Null(order.Phone);

        Assert.Equal(
            $"deleted-{user.Id}@hygiatrade.local",
            user.Email);

        Assert.Equal("Deleted user", user.Names);
        Assert.Equal(string.Empty, user.Phone);
        Assert.Equal(string.Empty, user.PasswordHash);
        Assert.Null(user.RefreshToken);
        Assert.Null(user.RefreshTokenExpiryTime);
        Assert.True(user.IsDeleted);
    }
}
