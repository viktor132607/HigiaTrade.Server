using HygiaTrade.Common.Responses.Gdpr;
using HygiaTrade.Common.Responses.Order;
using HygiaTrade.Common.Responses.OrderItem;
using HygiaTrade.Common.Responses.Review;
using HygiaTrade.Common.Responses.Users;

namespace HygiaTrade.Domain.Services;

public interface IGdprExportMapper
{
    GdprExportResponse Map(
        GdprExportData data,
        DateTime requestedAtUtc);
}

public sealed class GdprExportMapper : IGdprExportMapper
{
    public GdprExportResponse Map(
        GdprExportData data,
        DateTime requestedAtUtc) =>
        new()
        {
            RequestedAtUtc = requestedAtUtc,
            User = new UserResponse
            {
                Id = data.User.Id,
                Email = data.User.Email,
                Names = data.User.Names,
                Phone = data.User.Phone,
                Role = data.User.Role
            },
            Orders = data.Orders
                .Select(order => new OrderResponse
                {
                    Id = order.Id,
                    UserId = order.UserId,
                    OrderTotalPrice = order.OrderTotalPrice,
                    Names = order.Names,
                    PostalCode = order.PostalCode,
                    Country = order.Country,
                    City = order.City,
                    Address = order.Address,
                    Phone = order.Phone,
                    Status = order.Status,
                    CreatedOn = order.CreatedOn,
                    Items = order.Items
                        .Select(item => new OrderItemResponse
                        {
                            ProductId = item.ProductId,
                            SinglePrice = item.SinglePrice,
                            TotalPrice = item.TotalPrice,
                            Quantity = item.Quantity,
                            Title = item.Title,
                            PrimaryImageUri = item.PrimaryImageUri
                        })
                        .ToList()
                })
                .ToList(),
            WishlistProductIds = data.WishlistItems
                .Select(item => item.ProductId)
                .ToList(),
            Reviews = data.Reviews
                .Select(review => new ReviewResponse
                {
                    Id = review.Id,
                    Content = review.Content,
                    Rating = review.Rating,
                    CreatedOn = review.CreatedOn,
                    UserId = review.UserId,
                    UserNames = data.User.Names
                })
                .ToList()
        };
}
