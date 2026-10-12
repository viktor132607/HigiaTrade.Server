using HygiaTrade.API.Services;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class ImportedProductBrandSyncTests
{
    private static ApplicationDbContext CreateDb() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Backfill_CreatesMissingBrandsAndLogosForAlreadyImportedProducts()
    {
        await using ApplicationDbContext db = CreateDb();
        foreach (string name in new[] { "Ariel", "Persil", "Lenor", "Vanish", "Finish" })
        {
            db.Products.Add(new Product
            {
                Title = name + " test product",
                Description = "",
                MainImageUrl = "",
                Brand = name,
                CategoryId = Guid.NewGuid()
            });
        }
        await db.SaveChangesAsync();

        Assert.Equal(5, await ImportedProductBrandSync.BackfillExistingProductBrandsAsync(db));
        Assert.Equal(5, await db.Brands.CountAsync());
        Assert.All(await db.Brands.ToListAsync(), brand =>
            Assert.StartsWith("https://", brand.ThumbnailImageUrl));
        Assert.Equal(0, await ImportedProductBrandSync.BackfillExistingProductBrandsAsync(db));
    }

    [Fact]
    public async Task EnsureBrand_DoesNotReplaceCustomLogoOrDuplicateCaseInsensitiveBrand()
    {
        await using ApplicationDbContext db = CreateDb();
        db.Brands.Add(new Brand { Name = "Persil", ThumbnailImageUrl = "https://example.org/custom.png" });
        await db.SaveChangesAsync();
        Dictionary<string, Brand> existing = await ImportedProductBrandSync.LoadBrandsAsync(db, CancellationToken.None);

        bool changed = ImportedProductBrandSync.EnsureBrand(db, existing, " PERSIL ",
            new Dictionary<string, string> { ["Persil"] = "https://example.org/from-import.png" });
        Assert.False(changed);
        Assert.Single(await db.Brands.ToListAsync());
        Assert.Equal("https://example.org/custom.png", (await db.Brands.SingleAsync()).ThumbnailImageUrl);
    }

    [Fact]
    public async Task EnsureBrand_UsesImportSheetLogoForNewBrands()
    {
        await using ApplicationDbContext db = CreateDb();
        var existing = new Dictionary<string, Brand>(StringComparer.OrdinalIgnoreCase);
        var logos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BrandX"] = "https://example.org/brand-x.png"
        };

        Assert.True(ImportedProductBrandSync.EnsureBrand(db, existing, "BrandX", logos));
        await db.SaveChangesAsync();

        Assert.Equal("https://example.org/brand-x.png",
            (await db.Brands.SingleAsync()).ThumbnailImageUrl);
    }
}
