using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

/// <summary>
/// Keeps the Brands directory in sync with products and their Excel-provided logos.
/// Existing custom logos are never overwritten. No product migrations are needed.
/// </summary>
public static class ImportedProductBrandSync
{
    private sealed record BrandMetadata(string LogoUrl, string Description);

    // Verified image URLs for the 5 brands from the first example workbook.
    // Future imports are generic: other brands use the workbook's BrandLogos sheet.
    private static readonly IReadOnlyDictionary<string, BrandMetadata> Examples =
        new Dictionary<string, BrandMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["Ariel"] = new(
                "https://www.kindpng.com/picc/m/694-6943219_transparent-ariel-logo-png-png-download.png",
                "Ariel – перилни препарати, прахове, течни препарати и капсули за пране."),
            ["Persil"] = new(
                "https://www.vhv.rs/dpng/d/494-4944064_persil-colour-logo-png-transparent-png.png",
                "Persil – продукти за пране и грижа за текстила."),
            ["Lenor"] = new(
                "https://www.austriansupermarket.com/media/mageplaza/brand/Lenor-logo_brand.png",
                "Lenor – омекотители и ароматизиращи продукти за дрехи."),
            ["Vanish"] = new(
                "https://www.austriansupermarket.com/media/mageplaza/brand/Vanish-logo-brand.png",
                "Vanish – препарати за премахване на петна от тъкани."),
            ["Finish"] = new(
                "https://eu-images.contentstack.com/v3/assets/blt93c8bc7a598af9f0/blt5fd63f739566584d/6a243890bae584276093daca/10236_1_thumbnail_2525x1220_page1_199389-1-xxx_resized.jpg",
                "Finish – таблетки, препарати и добавки за съдомиялни машини.")
        };

    public static async Task<int> BackfillExistingProductBrandsAsync(
        ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        List<string?> names = await db.Products.AsNoTracking()
            .Where(product => !product.IsDeleted && product.Brand != null)
            .Select(product => product.Brand)
            .Distinct()
            .ToListAsync(cancellationToken);

        Dictionary<string, Brand> existing = await LoadBrandsAsync(db, cancellationToken);
        int changed = 0;
        foreach (string name in names.Where(name => !string.IsNullOrWhiteSpace(name))
                     .Select(name => name!.Trim())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (EnsureBrand(db, existing, name, null))
                changed++;
        }

        if (changed > 0) await db.SaveChangesAsync(cancellationToken);
        return changed;
    }

    public static async Task<Dictionary<string, Brand>> LoadBrandsAsync(
        ApplicationDbContext db, CancellationToken cancellationToken)
    {
        List<Brand> brands = await db.Brands
            .Where(brand => !brand.IsDeleted)
            .ToListAsync(cancellationToken);

        return brands
            .GroupBy(brand => brand.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(),
                StringComparer.OrdinalIgnoreCase);
    }

    public static bool EnsureBrand(
        ApplicationDbContext db,
        IDictionary<string, Brand> existing,
        string? rawName,
        IReadOnlyDictionary<string, string>? importedLogoUrls)
    {
        string name = (rawName ?? "").Trim();
        if (name.Length is < 2 or > 80) return false;

        string? preferredLogo = null;
        if (importedLogoUrls?.TryGetValue(name, out string? candidate) == true &&
            IsSafeImageUrl(candidate))
            preferredLogo = candidate;
        if (preferredLogo is null && Examples.TryGetValue(name, out BrandMetadata? known))
            preferredLogo = known.LogoUrl;

        if (existing.TryGetValue(name, out Brand? brand))
        {
            // A manually configured brand logo must always take precedence.
            if (!string.IsNullOrWhiteSpace(brand.ThumbnailImageUrl) || preferredLogo is null)
                return false;
            brand.ThumbnailImageUrl = preferredLogo;
            brand.ModifiedOn = DateTime.UtcNow;
            return true;
        }

        var created = new Brand
        {
            Name = name,
            ThumbnailImageUrl = preferredLogo,
            Description = Examples.TryGetValue(name, out BrandMetadata? details)
                ? details.Description
                : null
        };
        db.Brands.Add(created);
        existing[name] = created;
        return true;
    }

    private static bool IsSafeImageUrl(string url) =>
        url.Length <= 2000 &&
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
        uri.Scheme == Uri.UriSchemeHttps;
}
