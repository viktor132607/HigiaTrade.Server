using HygiaTrade.API.Services;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class InventoryStockAdderTests
{
    [Fact]
    public async Task AddAsync_Throws404_WhenProductDoesNotExist()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        var adder =
            new InventoryStockAdder(
                db,
                Mock.Of<IInventoryRequestValidator>(),
                Mock.Of<IInventoryClock>());

        InventoryServiceException ex =
            await Assert.ThrowsAsync<InventoryServiceException>(
                () => adder.AddAsync(
                    Guid.NewGuid(),
                    new InventoryStockRequest(
                        5,
                        "INV-001")));

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("Product not found.", ex.Message);
    }

    [Fact]
    public async Task AddAsync_StopsBeforeTransaction_WhenCapacityValidationFails()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        Category category = CreateCategory();

        Product product =
            CreateProduct(
                category,
                uint.MaxValue);

        db.AddRange(category, product);
        await db.SaveChangesAsync();

        var validator =
            new Mock<IInventoryRequestValidator>();

        validator
            .Setup(x => x.EnsureCanAdd(
                uint.MaxValue,
                1))
            .Throws(
                new InventoryServiceException(
                    400,
                    "The resulting quantity is too large."));

        var clock =
            new Mock<IInventoryClock>();

        var adder =
            new InventoryStockAdder(
                db,
                validator.Object,
                clock.Object);

        InventoryServiceException ex =
            await Assert.ThrowsAsync<InventoryServiceException>(
                () => adder.AddAsync(
                    product.Id,
                    new InventoryStockRequest(
                        1,
                        "INV-001")));

        Assert.Equal(400, ex.StatusCode);

        validator.Verify(
            x => x.EnsureCanAdd(
                uint.MaxValue,
                1),
            Times.Once);

        clock.VerifyGet(
            x => x.UtcNow,
            Times.Never);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"inventory-adder-{Guid.NewGuid():N}")
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
