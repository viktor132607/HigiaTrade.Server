using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class BrandRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_ReturnsSortedActiveBrandsWithActiveProductCounts()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        Category category =
            CategoryFor("Category");

        Brand alpha =
            new() { Name = "Alpha" };

        Brand beta =
            new() { Name = "Beta" };

        Brand deletedBrand =
            new()
            {
                Name = "Deleted",
                IsDeleted = true
            };

        Product alphaActive =
            ProductFor(
                category,
                "ALPHA",
                true,
                false);

        Product alphaInactive =
            ProductFor(
                category,
                "Alpha",
                false,
                false);

        Product alphaDeleted =
            ProductFor(
                category,
                "alpha",
                true,
                true);

        Product betaActive =
            ProductFor(
                category,
                "Beta",
                true,
                false);

        db.AddRange(
            category,
            beta,
            deletedBrand,
            alpha,
            alphaActive,
            alphaInactive,
            alphaDeleted,
            betaActive);

        await db.SaveChangesAsync();

        var repository =
            new BrandRepository(db);

        IReadOnlyList<BrandListItem> result =
            await repository.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal(1, result[0].ProductCount);
        Assert.Equal("Beta", result[1].Name);
        Assert.Equal(1, result[1].ProductCount);
    }

    [Fact]
    public async Task ExistsByNameAsync_IsCaseInsensitiveAndSupportsExcludedId()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        Brand brand =
            new() { Name = "Acme" };

        Brand deleted =
            new()
            {
                Name = "Ghost",
                IsDeleted = true
            };

        db.AddRange(brand, deleted);
        await db.SaveChangesAsync();

        var repository =
            new BrandRepository(db);

        Assert.True(
            await repository.ExistsByNameAsync(
                "ACME"));

        Assert.False(
            await repository.ExistsByNameAsync(
                "acme",
                brand.Id));

        Assert.False(
            await repository.ExistsByNameAsync(
                "ghost"));
    }

    [Fact]
    public async Task GetTrackedAsync_IgnoresDeletedBrands()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        Brand active =
            new() { Name = "Active" };

        Brand deleted =
            new()
            {
                Name = "Deleted",
                IsDeleted = true
            };

        db.AddRange(active, deleted);
        await db.SaveChangesAsync();

        var repository =
            new BrandRepository(db);

        Assert.Same(
            active,
            await repository.GetTrackedAsync(
                active.Id));

        Assert.Null(
            await repository.GetTrackedAsync(
                deleted.Id));
    }

    [Fact]
    public async Task ProductCounts_DistinguishAssignedFromActive()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        Category category =
            CategoryFor("Category");

        Product active =
            ProductFor(
                category,
                "Acme",
                true,
                false);

        Product inactive =
            ProductFor(
                category,
                "ACME",
                false,
                false);

        Product deleted =
            ProductFor(
                category,
                "acme",
                true,
                true);

        db.AddRange(
            category,
            active,
            inactive,
            deleted);

        await db.SaveChangesAsync();

        var repository =
            new BrandRepository(db);

        Assert.Equal(
            1,
            await repository
                .GetActiveProductCountAsync(
                    "AcMe"));

        Assert.Equal(
            2,
            await repository
                .GetAssignedProductCountAsync(
                    "AcMe"));
    }

    [Fact]
    public async Task RenameProductsAsync_RenamesOnlyNonDeletedMatchingProducts()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        Category category =
            CategoryFor("Category");

        Product match =
            ProductFor(
                category,
                "Old",
                true,
                false);

        Product deleted =
            ProductFor(
                category,
                "OLD",
                true,
                true);

        Product other =
            ProductFor(
                category,
                "Other",
                true,
                false);

        db.AddRange(
            category,
            match,
            deleted,
            other);

        await db.SaveChangesAsync();

        DateTime modifiedOn =
            new(
                2026,
                9,
                25,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var repository =
            new BrandRepository(db);

        await repository.RenameProductsAsync(
            "old",
            "New",
            modifiedOn);

        Assert.Equal("New", match.Brand);
        Assert.Equal(modifiedOn, match.ModifiedOn);
        Assert.Equal("OLD", deleted.Brand);
        Assert.Equal("Other", other.Brand);
    }

    [Fact]
    public async Task AddAndSaveChangesAsync_PersistBrand()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        Brand brand =
            new() { Name = "Acme" };

        var repository =
            new BrandRepository(db);

        repository.Add(brand);
        await repository.SaveChangesAsync();

        Assert.True(
            await db.Brands.AnyAsync(
                x => x.Id == brand.Id));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"brand-repository-{Guid.NewGuid():N}")
                .Options;

        return new ApplicationDbContext(options);
    }

    private static Category CategoryFor(
        string name) =>
        new()
        {
            Name = name,
            ImageUri = null
        };

    private static Product ProductFor(
        Category category,
        string brand,
        bool isActive,
        bool isDeleted) =>
        new()
        {
            Title = Guid.NewGuid().ToString("N"),
            Brand = brand,
            Description = "Description",
            MainImageUrl = "image",
            IsActive = isActive,
            RegularPrice = 10m,
            DiscountPercentage = 0,
            DiscountedPrice = 0m,
            WholesalePrice = 0m,
            WholesaleMinQuantity = 0,
            VatRate = 20m,
            Quantity = 1,
            CategoryId = category.Id,
            Category = category,
            IsDeleted = isDeleted
        };
}
