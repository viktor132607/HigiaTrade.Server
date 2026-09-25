using HygiaTrade.API.Services;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class InventoryReaderTests
{
    [Fact]
    public async Task GetProductQuantityAsync_ReturnsQuantityForActiveProduct()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        Category category = CreateCategory();

        Product product =
            CreateProduct(
                category,
                17);

        db.AddRange(category, product);
        await db.SaveChangesAsync();

        var reader =
            new InventoryReader(db);

        uint? result =
            await reader.GetProductQuantityAsync(
                product.Id);

        Assert.Equal(17u, result);
    }

    [Fact]
    public async Task GetProductQuantityAsync_ReturnsNullForDeletedProduct()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        Category category = CreateCategory();

        Product product =
            CreateProduct(
                category,
                17);

        product.IsDeleted = true;

        db.AddRange(category, product);
        await db.SaveChangesAsync();

        var reader =
            new InventoryReader(db);

        uint? result =
            await reader.GetProductQuantityAsync(
                product.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductQuantityAsync_ReturnsNullForMissingProduct()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        var reader =
            new InventoryReader(db);

        uint? result =
            await reader.GetProductQuantityAsync(
                Guid.NewGuid());

        Assert.Null(result);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"inventory-reader-{Guid.NewGuid():N}")
                .Options;

        return new ApplicationDbContext(options);
    }

    private static Category CreateCategory() =>
        new()
        {
            Name = "Category",
            ImageUri = null
        };

    private static Product CreateProduct(
        Category category,
        uint quantity) =>
        new()
        {
            Title = "Product",
            Brand = null,
            Description = "Description",
            MainImageUrl = "image",
            RegularPrice = 10m,
            DiscountPercentage = 0,
            DiscountedPrice = 0m,
            WholesalePrice = 0m,
            WholesaleMinQuantity = 0,
            VatRate = 20m,
            Quantity = quantity,
            CategoryId = category.Id,
            Category = category
        };
}
