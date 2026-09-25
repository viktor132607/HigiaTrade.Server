using HygiaTrade.API.Controllers;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Services;

public interface INewProductsService
{
    Task<NewProductsPageResponse> GetAsync(
        int pageNumber,
        int pageSize);

    Task<NewProductStatusDto> GetStatusAsync(Guid productId);

    Task<NewProductStatusDto> UpdateStatusAsync(
        Guid productId,
        UpdateNewProductStatusRequest request);
}

public sealed class NewProductsServiceException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class NewProductsService(
    INewProductStatusRepository repository,
    INewProductsPolicy policy,
    INewProductsClock clock,
    IProductService productService) : INewProductsService
{
    public async Task<NewProductsPageResponse> GetAsync(
        int pageNumber,
        int pageSize)
    {
        NewProductsPage page =
            policy.NormalizePage(
                pageNumber,
                pageSize);

        IReadOnlyList<Guid> activeProductIds =
            await repository.GetActiveProductIdsAsync();

        int totalCount = activeProductIds.Count;

        IEnumerable<Guid> pageIds =
            activeProductIds
                .Skip(
                    (page.PageNumber - 1) *
                    page.PageSize)
                .Take(page.PageSize);

        List<object> products = [];

        foreach (Guid productId in pageIds)
        {
            ProductResponse? product =
                await productService.GetByIdAsync(
                    productId);

            if (product is not null &&
                product.IsActive)
            {
                products.Add(product);
            }
        }

        return new NewProductsPageResponse(
            products,
            totalCount);
    }

    public async Task<NewProductStatusDto> GetStatusAsync(
        Guid productId)
    {
        NewProductStatusRecord? status =
            await repository.ReadAsync(productId);

        return policy.CreateStatus(
            status,
            clock.UtcNow);
    }

    public async Task<NewProductStatusDto> UpdateStatusAsync(
        Guid productId,
        UpdateNewProductStatusRequest request)
    {
        if (!await repository.ProductExistsAsync(
                productId))
        {
            throw new NewProductsServiceException(
                StatusCodes.Status404NotFound,
                "Product not found.");
        }

        if (!request.IsNewProduct)
        {
            await repository.DeleteAsync(
                productId);

            return policy.CreateInactiveStatus();
        }

        policy.ValidateDisplayDays(
            request.DisplayDays);

        DateTime activeUntilUtc =
            clock.UtcNow.AddDays(
                request.DisplayDays);

        await repository.UpsertAsync(
            productId,
            request.DisplayDays,
            activeUntilUtc);

        return await GetStatusAsync(productId);
    }
}
