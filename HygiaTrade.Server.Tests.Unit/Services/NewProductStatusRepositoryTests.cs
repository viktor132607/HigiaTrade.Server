using HygiaTrade.API.Services;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class NewProductStatusRepositoryTests
{
    [Fact]
    public async Task ProductExistsAsync_OnlyCountsNonDeletedProducts()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        Category category =
            new()
            {
                Name = "Category",
                ImageUri = null
            };

        Product active =
            ProductFor(
                "Active",
                category);

        Product deleted =
            ProductFor(
                "Deleted",
                category);

        deleted.IsDeleted = true;

        db.AddRange(
            category,
            active,
            deleted);

        await db.SaveChangesAsync();

        var repository =
            new NewProductStatusRepository(db);

        Assert.True(
            await repository.ProductExistsAsync(
                active.Id));

        Assert.False(
            await repository.ProductExistsAsync(
                deleted.Id));

        Assert.False(
            await repository.ProductExistsAsync(
                Guid.NewGuid()));
    }

    private static ApplicationDbContext CreateDbContext()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"new-products-{Guid.NewGuid():N}")
                .Options;

        return new ApplicationDbContext(options);
    }

    private static Product ProductFor(
        string title,
        Category category) =>
        new()
        {
            Title = title,
            Brand = null,
            Description = "Description",
            MainImageUrl = "image",
            RegularPrice = 10m,
            DiscountPercentage = 0,
            DiscountedPrice = 0m,
            WholesalePrice = 0m,
            WholesaleMinQuantity = 0,
            VatRate = 20m,
            Quantity = 5,
            CategoryId = category.Id,
            Category = category
        };
}
