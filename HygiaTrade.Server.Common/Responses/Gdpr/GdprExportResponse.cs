using HygiaTrade.Common.Responses.Order;
using HygiaTrade.Common.Responses.Review;
using HygiaTrade.Common.Responses.Users;

namespace HygiaTrade.Common.Responses.Gdpr;

public class GdprExportResponse
{
    public DateTime RequestedAtUtc { get; set; }

    public UserResponse? User { get; set; }

    public ICollection<OrderResponse> Orders { get; set; } = new List<OrderResponse>();

    public ICollection<Guid> WishlistProductIds { get; set; } = new List<Guid>();

    public ICollection<ReviewResponse> Reviews { get; set; } = new List<ReviewResponse>();
}
