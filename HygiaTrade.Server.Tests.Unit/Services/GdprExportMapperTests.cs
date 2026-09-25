using HygiaTrade.Core.Enums;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class GdprExportMapperTests
{
    [Fact]
    public void Map_MapsUserOrdersWishlistAndReviews()
    {
        Guid userId = Guid.NewGuid();
        Guid productId = Guid.NewGuid();

        User user = new()
        {
            Id = userId,
            Email = "user@example.com",
            Names = "Test User",
            Phone = "123",
            Role = "User",
            PasswordHash = "hash"
        };

        Order order = new()
        {
            UserId = userId,
            OrderTotalPrice = 42m,
            Names = "Order User",
            PostalCode = "9000",
            Country = "BG",
            City = "Varna",
            Address = "Address",
            Phone = "456",
            Status = OrderStatus.Delivered,
            CreatedOn =
                new DateTime(
                    2026,
                    9,
                    20,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc)
        };

        OrderItem item = new()
        {
            OrderId = order.Id,
            Order = order,
            ProductId = productId,
            Product = null!,
            Quantity = 2,
            SinglePrice = 10m,
            TotalPrice = 20m,
            Title = "Product",
            PrimaryImageUri = "image"
        };

        order.Items.Add(item);

        WishlistItem wishlist = new()
        {
            UserId = userId,
            User = user,
            ProductId = productId,
            Product = null!
        };

        Review review = new()
        {
            UserId = userId,
            User = user,
            ProductId = productId,
            Product = null!,
            Content = "Good",
            Rating = 5,
            CreatedOn =
                new DateTime(
                    2026,
                    9,
                    21,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc)
        };

        DateTime requestedAt =
            new(
                2026,
                9,
                25,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var data = new GdprExportData(
            user,
            [order],
            [wishlist],
            [review]);

        var mapper = new GdprExportMapper();

        var result =
            mapper.Map(data, requestedAt);

        Assert.Equal(requestedAt, result.RequestedAtUtc);

        Assert.NotNull(result.User);
        Assert.Equal(userId, result.User.Id);
        Assert.Equal("user@example.com", result.User.Email);
        Assert.Equal("Test User", result.User.Names);
        Assert.Equal("123", result.User.Phone);
        Assert.Equal("User", result.User.Role);

        var mappedOrder = Assert.Single(result.Orders);
        Assert.Equal(order.Id, mappedOrder.Id);
        Assert.Equal(userId, mappedOrder.UserId);
        Assert.Equal(42m, mappedOrder.OrderTotalPrice);
        Assert.Equal("Order User", mappedOrder.Names);
        Assert.Equal("9000", mappedOrder.PostalCode);
        Assert.Equal("BG", mappedOrder.Country);
        Assert.Equal("Varna", mappedOrder.City);
        Assert.Equal("Address", mappedOrder.Address);
        Assert.Equal("456", mappedOrder.Phone);
        Assert.Equal(OrderStatus.Delivered, mappedOrder.Status);
        Assert.Equal(order.CreatedOn, mappedOrder.CreatedOn);

        var mappedItem = Assert.Single(mappedOrder.Items);
        Assert.Equal(productId, mappedItem.ProductId);
        Assert.Equal(10m, mappedItem.SinglePrice);
        Assert.Equal(20m, mappedItem.TotalPrice);
        Assert.Equal(2, mappedItem.Quantity);
        Assert.Equal("Product", mappedItem.Title);
        Assert.Equal("image", mappedItem.PrimaryImageUri);

        Assert.Equal(
            [productId],
            result.WishlistProductIds);

        var mappedReview = Assert.Single(result.Reviews);
        Assert.Equal(review.Id, mappedReview.Id);
        Assert.Equal("Good", mappedReview.Content);
        Assert.Equal((byte)5, mappedReview.Rating);
        Assert.Equal(review.CreatedOn, mappedReview.CreatedOn);
        Assert.Equal(userId, mappedReview.UserId);
        Assert.Equal("Test User", mappedReview.UserNames);
    }
}
