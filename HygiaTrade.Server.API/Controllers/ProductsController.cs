using HygiaTrade.API.Helpers;
using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Core.Pages;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(
    IProductService productService,
    ApplicationDbContext db) : ControllerBase
{
    private const string SiteDefaultImageUrl = "/higiqlogo.png";

    [HttpGet]
    public async Task<IActionResult> GetAllAsync([FromQuery] SearchProductsRequest? request)
    {
        SearchProductsRequest searchRequest = request ?? new SearchProductsRequest();
        searchRequest.IncludeInactive = User.IsInRole(Roles.Admin);

        return await ControllerProcessor.ProcessAsync(
            async () =>
            {
                Paginated<ProductsResponse> result = await productService.SearchProductsAsync(searchRequest);
                await ResolveDefaultImagesAsync(result.Items ?? Enumerable.Empty<ProductsResponse>());
                return result;
            },
            this,
            true);
    }

    [HttpGet("best-sellers")]
    public async Task<IActionResult> GetBestSellersAsync(int numOfBestSellers)
    {
        return await ControllerProcessor.ProcessAsync(
            async () =>
            {
                IEnumerable<ProductResponse>? products = await productService.GetBestSellersAsync(numOfBestSellers);
                List<ProductResponse> resolved = products?.ToList() ?? [];
                await ResolveDefaultImagesAsync(resolved);
                return resolved;
            },
            this,
            true);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        return await ControllerProcessor.ProcessAsync(
            async () =>
            {
                ProductResponse? product = await productService.GetByIdAsync(id);
                if (product is not null)
                {
                    await ResolveDefaultImagesAsync([product]);
                }

                return product;
            },
            this);
    }

    [HttpGet("{id}/price")]
    public async Task<IActionResult> GetPriceQuoteAsync(Guid id, [FromQuery] int quantity = 1)
    {
        return await ControllerProcessor.ProcessAsync(
            () => productService.GetPriceQuoteAsync(id, quantity),
            this);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateProductRequest request)
    {
        return await ControllerProcessor.ProcessAsync(
            async () =>
            {
                await StripVirtualDefaultImagesAsync(request);
                ProductResponse? product = await productService.CreateAsync(request);
                if (product is not null)
                {
                    await ResolveDefaultImagesAsync([product]);
                }

                return product;
            },
            this,
            true);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateProductRequest request)
    {
        return await ControllerProcessor.ProcessAsync(
            async () =>
            {
                await StripVirtualDefaultImagesAsync(request);
                ProductResponse? product = await productService.UpdateAsync(request);
                if (product is not null)
                {
                    await ResolveDefaultImagesAsync([product]);
                }

                return product;
            },
            this,
            true);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        return await ControllerProcessor.ProcessAsync<object>(
            async () => await productService.DeleteAsync(id), this);
    }

    private async Task StripVirtualDefaultImagesAsync(CreateProductRequest request)
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

    private async Task StripVirtualDefaultImagesAsync(UpdateProductRequest request)
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

    private async Task ResolveDefaultImagesAsync(IEnumerable<ProductResponse> products)
    {
        List<ProductResponse> materialized = products.ToList();
        Dictionary<string, string> brandImages = await LoadBrandImagesAsync(materialized.Select(product => product.Brand));

        foreach (ProductResponse product in materialized)
        {
            string? brandImage = GetBrandImage(product.Brand, brandImages);
            bool usesDefault = IsVirtualDefaultImage(product.MainImageUrl, brandImage);
            product.UsesDefaultImage = usesDefault;

            if (usesDefault)
            {
                product.MainImageUrl = !string.IsNullOrWhiteSpace(brandImage)
                    ? brandImage
                    : SiteDefaultImageUrl;
            }

            product.SecondaryImages = product.SecondaryImages
                .Where(image => !IsVirtualDefaultImage(image.Uri, brandImage))
                .ToList();
        }
    }

    private async Task ResolveDefaultImagesAsync(IEnumerable<ProductsResponse> products)
    {
        List<ProductsResponse> materialized = products.ToList();
        Dictionary<string, string> brandImages = await LoadBrandImagesAsync(materialized.Select(product => product.Brand));

        foreach (ProductsResponse product in materialized)
        {
            string? brandImage = GetBrandImage(product.Brand, brandImages);
            bool usesDefault = IsVirtualDefaultImage(product.MainImageUrl, brandImage);
            product.UsesDefaultImage = usesDefault;

            if (usesDefault)
            {
                product.MainImageUrl = !string.IsNullOrWhiteSpace(brandImage)
                    ? brandImage
                    : SiteDefaultImageUrl;
            }

            product.SecondaryImages = product.SecondaryImages
                .Where(image => !IsVirtualDefaultImage(image.Uri, brandImage))
                .ToList();
        }
    }

    private async Task<string?> GetBrandImageAsync(string? brandName)
    {
        if (string.IsNullOrWhiteSpace(brandName))
        {
            return null;
        }

        Dictionary<string, string> images = await LoadBrandImagesAsync([brandName]);
        return GetBrandImage(brandName, images);
    }

    private async Task<Dictionary<string, string>> LoadBrandImagesAsync(IEnumerable<string?> brandNames)
    {
        HashSet<string> requested = brandNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (requested.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var brands = await db.Brands
            .AsNoTracking()
            .Where(brand => !brand.IsDeleted && brand.ThumbnailImageUrl != null)
            .Select(brand => new { brand.Name, brand.ThumbnailImageUrl })
            .ToListAsync();

        return brands
            .Where(brand => requested.Contains(brand.Name.Trim()) && !string.IsNullOrWhiteSpace(brand.ThumbnailImageUrl))
            .GroupBy(brand => brand.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First().ThumbnailImageUrl!.Trim(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string? GetBrandImage(string? brandName, IReadOnlyDictionary<string, string> brandImages)
    {
        if (string.IsNullOrWhiteSpace(brandName))
        {
            return null;
        }

        return brandImages.TryGetValue(brandName.Trim(), out string? image)
            ? image
            : null;
    }

    private static bool IsVirtualDefaultImage(string? imageUrl, string? brandImage)
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

        return !string.IsNullOrWhiteSpace(brandImage) &&
               string.Equals(value, brandImage.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSiteDefaultImage(string imageUrl)
    {
        string withoutFragment = imageUrl.Split('#', 2)[0];
        string withoutQuery = withoutFragment.Split('?', 2)[0].TrimEnd('/');
        return withoutQuery.EndsWith("/higiqlogo.png", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(withoutQuery, "higiqlogo.png", StringComparison.OrdinalIgnoreCase);
    }
}
