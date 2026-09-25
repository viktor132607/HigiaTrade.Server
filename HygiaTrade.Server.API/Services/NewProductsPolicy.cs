using HygiaTrade.API.Controllers;

namespace HygiaTrade.API.Services;

public sealed record NewProductsPage(
    int PageNumber,
    int PageSize);

public interface INewProductsPolicy
{
    NewProductsPage NormalizePage(
        int pageNumber,
        int pageSize);

    void ValidateDisplayDays(int displayDays);

    NewProductStatusDto CreateStatus(
        NewProductStatusRecord? status,
        DateTime utcNow);

    NewProductStatusDto CreateInactiveStatus();
}

public sealed class NewProductsPolicy
    : INewProductsPolicy
{
    private const int DefaultDisplayDays = 14;

    public NewProductsPage NormalizePage(
        int pageNumber,
        int pageSize) =>
        new(
            Math.Max(1, pageNumber),
            Math.Clamp(pageSize, 1, 200));

    public void ValidateDisplayDays(
        int displayDays)
    {
        if (displayDays is < 1 or > 365)
        {
            throw new NewProductsServiceException(
                StatusCodes.Status400BadRequest,
                "DisplayDays must be between 1 and 365.");
        }
    }

    public NewProductStatusDto CreateStatus(
        NewProductStatusRecord? status,
        DateTime utcNow)
    {
        if (status is null)
        {
            return CreateInactiveStatus();
        }

        return new NewProductStatusDto(
            true,
            status.DisplayDays,
            status.ActiveUntilUtc,
            status.ActiveUntilUtc > utcNow);
    }

    public NewProductStatusDto CreateInactiveStatus() =>
        new(
            false,
            DefaultDisplayDays,
            null,
            false);
}
