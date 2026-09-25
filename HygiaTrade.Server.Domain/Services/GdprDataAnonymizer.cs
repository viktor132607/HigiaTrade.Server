namespace HygiaTrade.Domain.Services;

public interface IGdprDataAnonymizer
{
    void Anonymize(GdprDeletionData data);
}

public sealed class GdprDataAnonymizer
    : IGdprDataAnonymizer
{
    public void Anonymize(GdprDeletionData data)
    {
        foreach (var wishlistItem in data.WishlistItems)
        {
            wishlistItem.IsDeleted = true;
        }

        foreach (var review in data.Reviews)
        {
            review.IsDeleted = true;
        }

        foreach (var order in data.Orders)
        {
            order.Names = "Deleted user";
            order.PostalCode = null;
            order.Country = null;
            order.City = null;
            order.Address = null;
            order.Phone = null;
        }

        data.User.Email =
            $"deleted-{data.User.Id}@hygiatrade.local";

        data.User.Names = "Deleted user";
        data.User.Phone = string.Empty;
        data.User.PasswordHash = string.Empty;
        data.User.RefreshToken = null;
        data.User.RefreshTokenExpiryTime = null;
        data.User.IsDeleted = true;
    }
}
