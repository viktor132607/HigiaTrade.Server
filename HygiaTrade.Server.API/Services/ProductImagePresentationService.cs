using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Data;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public interface IProductImagePresentationService
{
    Task PrepareForPersistenceAsync(CreateProductRequest request);
    Task PrepareForPersistenceAsync(UpdateProductRequest request);
    Task ResolveAsync(IEnumerable<ProductResponse> products);
    Task ResolveAsync(IEnumerable<ProductsResponse> products);
}

public sealed class ProductImagePresentationService(
    ApplicationDbContext db) : IProductImagePresentationService
{
    private const string SiteDefaultImageUrl = "/higiqlogo.png";

    public async Task PrepareForPersistenceAsync(
        CreateProductRequest request)
    {
        string? brandImage = await GetBrandImageAsync(request.Brand);

        if (IsVirtualDefaultImage(request.MainImageUrl, brandImage))
        {
            request.MainImageUrl = string.Empty;
        }

        request.SecondaryImages = request.SecondaryImages
            .Where(image => !IsVirtualDefaultImage(image.Uri, brandImage))
            .ToList();
    }

    public async Task PrepareForPersistenceAsync(
        UpdateProductRequest request)
    {
        string? brandImage = await GetBrandImageAsync(request.Brand);

        if (IsVirtualDefaultImage(request.MainImageUrl, brandImage))
        {
            request.MainImageUrl = string.Empty;
        }

        request.SecondaryImages = request.SecondaryImages
            .Where(image => !IsVirtualDefaultImage(image.Uri, brandImage))
            .ToList();
    }

    public async Task ResolveAsync(
        IEnumerable<ProductResponse> products)
    {
        List<ProductResponse> materialized = products.ToList();

        Dictionary<string, string> brandImages =
            await LoadBrandImagesAsync(
                materialized.Select(product => product.Brand));

        foreach (ProductResponse product in materialized)
        {
            string? brandImage =
                GetBrandImage(product.Brand, brandImages);

            bool usesDefault =
                IsVirtualDefaultImage(
                    product.MainImageUrl,
                    brandImage);

            product.UsesDefaultImage = usesDefault;

            if (usesDefault)
            {
                product.MainImageUrl =
                    !string.IsNullOrWhiteSpace(brandImage)
                        ? brandImage
                        : SiteDefaultImageUrl;
            }

            product.SecondaryImages = product.SecondaryImages
                .Where(image =>
                    !IsVirtualDefaultImage(
                        image.Uri,
                        brandImage))
                .ToList();
        }
    }

    public async Task ResolveAsync(
        IEnumerable<ProductsResponse> products)
    {
        List<ProductsResponse> materialized = products.ToList();

        Dictionary<string, string> brandImages =
            await LoadBrandImagesAsync(
                materialized.Select(product => product.Brand));

        foreach (ProductsResponse product in materialized)
        {
            string? brandImage =
                GetBrandImage(product.Brand, brandImages);

            bool usesDefault =
                IsVirtualDefaultImage(
                    product.MainImageUrl,
                    brandImage);

            product.UsesDefaultImage = usesDefault;

            if (usesDefault)
            {
                product.MainImageUrl =
                    !string.IsNullOrWhiteSpace(brandImage)
                        ? brandImage
                        : SiteDefaultImageUrl;
            }

            product.SecondaryImages = product.SecondaryImages
                .Where(image =>
                    !IsVirtualDefaultImage(
                        image.Uri,
                        brandImage))
                .ToList();
        }
    }

    private async Task<string?> GetBrandImageAsync(
        string? brandName)
    {
        if (string.IsNullOrWhiteSpace(brandName))
        {
            return null;
        }

        Dictionary<string, string> images =
            await LoadBrandImagesAsync([brandName]);

        return GetBrandImage(brandName, images);
    }

    private async Task<Dictionary<string, string>>
        LoadBrandImagesAsync(
            IEnumerable<string?> brandNames)
    {
        HashSet<string> requested = brandNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (requested.Count == 0)
        {
            return new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        }

        var brands = await db.Brands
            .AsNoTracking()
            .Where(brand =>
                !brand.IsDeleted &&
                brand.ThumbnailImageUrl != null)
            .Select(brand => new
            {
                brand.Name,
                brand.ThumbnailImageUrl
            })
            .ToListAsync();

        return brands
            .Where(brand =>
                requested.Contains(brand.Name.Trim()) &&
                !string.IsNullOrWhiteSpace(
                    brand.ThumbnailImageUrl))
            .GroupBy(
                brand => brand.Name.Trim(),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                    group.First().ThumbnailImageUrl!.Trim(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string? GetBrandImage(
        string? brandName,
        IReadOnlyDictionary<string, string> brandImages)
    {
        if (string.IsNullOrWhiteSpace(brandName))
        {
            return null;
        }

        return brandImages.TryGetValue(
            brandName.Trim(),
            out string? image)
            ? image
            : null;
    }

    private static bool IsVirtualDefaultImage(
        string? imageUrl,
        string? brandImage)
    {
        string value = imageUrl?.Trim() ?? string.Empty;

        if (value.Length == 0)
        {
            return true;
        }

        if (IsSiteDefaultImage(value))
        {
            return true;
        }

        return
            !string.IsNullOrWhiteSpace(brandImage) &&
            string.Equals(
                value,
                brandImage.Trim(),
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSiteDefaultImage(
        string imageUrl)
    {
        string withoutFragment =
            imageUrl.Split('#', 2)[0];

        string withoutQuery =
            withoutFragment
                .Split('?', 2)[0]
                .TrimEnd('/');

        return
            withoutQuery.EndsWith(
                "/higiqlogo.png",
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                withoutQuery,
                "higiqlogo.png",
                StringComparison.OrdinalIgnoreCase);
    }
}
