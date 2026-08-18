using Microsoft.EntityFrameworkCore;
using HygiaTrade.Common.Responses.Gdpr;
using HygiaTrade.Common.Responses.Order;
using HygiaTrade.Common.Responses.OrderItem;
using HygiaTrade.Common.Responses.Review;
using HygiaTrade.Common.Responses.Users;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public class GdprService(
    ApplicationDbContext context,
    IAuthService authService,
    IUserRepository userRepository) : IGdprService
{
    public async Task<GdprExportResponse> ExportCurrentUserDataAsync()
    {
        Guid userId = await GetCurrentUserIdAsync();

        User? user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
        {
            throw new AppException("User not found.").SetStatusCode(404);
        }

        List<Order> orders = await context.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync();

        List<WishlistItem> wishlistItems = await context.WishlistItems
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .ToListAsync();

        List<Review> reviews = await context.Reviews
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedOn)
            .ToListAsync();

        return new GdprExportResponse
        {
            RequestedAtUtc = DateTime.UtcNow,
            User = new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                Names = user.Names,
                Phone = user.Phone,
                Role = user.Role
            },
            Orders = orders.Select(order => new OrderResponse
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
                Items = order.Items.Select(item => new OrderItemResponse
                {
                    ProductId = item.ProductId,
                    SinglePrice = item.SinglePrice,
                    TotalPrice = item.TotalPrice,
                    Quantity = item.Quantity,
                    Title = item.Title,
                    PrimaryImageUri = item.PrimaryImageUri
                }).ToList()
            }).ToList(),
            WishlistProductIds = wishlistItems.Select(item => item.ProductId).ToList(),
            Reviews = reviews.Select(review => new ReviewResponse
            {
                Id = review.Id,
                Content = review.Content,
                Rating = review.Rating,
                CreatedOn = review.CreatedOn,
                UserId = review.UserId,
                UserNames = user.Names
            }).ToList()
        };
    }

    public async Task<GdprDeleteResponse> DeleteCurrentUserDataAsync()
    {
        Guid userId = await GetCurrentUserIdAsync();

        User? user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null)
        {
            throw new AppException("User not found.").SetStatusCode(404);
        }

        List<WishlistItem> wishlistItems = await context.WishlistItems
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .ToListAsync();

        foreach (WishlistItem wishlistItem in wishlistItems)
        {
            wishlistItem.IsDeleted = true;
        }

        List<Review> reviews = await context.Reviews
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .ToListAsync();

        foreach (Review review in reviews)
        {
            review.IsDeleted = true;
        }

        List<Order> orders = await context.Orders
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .ToListAsync();

        foreach (Order order in orders)
        {
            order.Names = "Deleted user";
            order.PostalCode = null;
            order.Country = null;
            order.City = null;
            order.Address = null;
            order.Phone = null;
        }

        user.Email = $"deleted-{user.Id}@hygiatrade.local";
        user.Names = "Deleted user";
        user.Phone = string.Empty;
        user.PasswordHash = string.Empty;
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        user.IsDeleted = true;

        await context.SaveChangesAsync();

        return new GdprDeleteResponse
        {
            Deleted = true,
            Message = "Your personal account data has been anonymized and marked for deletion."
        };
    }

    private async Task<Guid> GetCurrentUserIdAsync()
    {
        string? currentUserId = await authService.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw new AppException("Unauthorized").SetStatusCode(401);
        }

        Guid userId = Guid.Parse(currentUserId);
        User? user = await userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new AppException("User not found.").SetStatusCode(404);
        }

        return userId;
    }
}
