using HygiaTrade.API.Services;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class ProductExcelImportServiceTests
{
    [Fact]
    public async Task ImportAsync_CreatesProduct_WithPricingStockAndCategory()
    {
        await using ApplicationDbContext db = CreateDb();
        Category category = await AddCategoryAsync(db, "Tools");

        var reader = new Mock<IProductExcelWorkbookReader>();
        reader.Setup(x => x.Read(It.IsAny<Stream>()))
            .Returns([
                Row(
                    title: "Widget",
                    category: "Tools",
                    regularPrice: "12,50",
                    brand: " Acme ",
                    discountPercentage: "10",
                    wholesalePrice: "9.25",
                    wholesaleMinQuantity: "5",
                    vatRate: "20",
                    quantity: "7")
            ]);

        var service = CreateService(db, reader.Object);

        ProductExcelImportResult result =
            await service.ImportAsync(
                ExcelFile(),
                updateExisting: true,
                CancellationToken.None);

        Assert.Equal(1, result.Created);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Failed);

        Product product = await db.Products.SingleAsync();
        Assert.Equal("Widget", product.Title);
        Assert.Equal("Acme", product.Brand);
        Assert.Equal(category.Id, product.CategoryId);
        Assert.Equal(12.50m, product.RegularPrice);
        Assert.Equal(10, product.DiscountPercentage);
        Assert.Equal(11.25m, product.DiscountedPrice);
        Assert.Equal(9.25m, product.WholesalePrice);
        Assert.Equal((uint)5, product.WholesaleMinQuantity);
        Assert.Equal((uint)7, product.Quantity);
        Assert.Equal(20m, product.VatRate);
    }

    [Fact]
    public async Task ImportAsync_UpdatesExistingProduct_WhenEnabled()
    {
        await using ApplicationDbContext db = CreateDb();
        Category oldCategory = await AddCategoryAsync(db, "Old");
        Category newCategory = await AddCategoryAsync(db, "New");

        var existing = new Product
        {
            Title = "Widget",
            Brand = "Acme",
            Description = "Old description",
            MainImageUrl = "",
            CategoryId = oldCategory.Id,
            Quantity = 1,
            RegularPrice = 5m
        };

        db.Products.Add(existing);
        await db.SaveChangesAsync();

        var reader = new Mock<IProductExcelWorkbookReader>();
        reader.Setup(x => x.Read(It.IsAny<Stream>()))
            .Returns([
                Row(
                    title: "widget",
                    category: newCategory.Id.ToString(),
                    regularPrice: "20",
                    brand: "ACME",
                    description: "Updated",
                    quantity: "12",
                    isActive: "false")
            ]);

        ProductExcelImportResult result =
            await CreateService(db, reader.Object)
                .ImportAsync(
                    ExcelFile(),
                    updateExisting: true,
                    CancellationToken.None);

        Assert.Equal(0, result.Created);
        Assert.Equal(1, result.Updated);

        Product product = await db.Products.SingleAsync();
        Assert.Equal(existing.Id, product.Id);
        Assert.Equal("widget", product.Title);
        Assert.Equal("ACME", product.Brand);
        Assert.Equal("Updated", product.Description);
        Assert.Equal(newCategory.Id, product.CategoryId);
        Assert.Equal((uint)12, product.Quantity);
        Assert.Equal(20m, product.RegularPrice);
        Assert.False(product.IsActive);
    }

    [Fact]
    public async Task ImportAsync_SkipsExistingProduct_WhenUpdatesDisabled()
    {
        await using ApplicationDbContext db = CreateDb();
        Category category = await AddCategoryAsync(db, "Tools");

        db.Products.Add(new Product
        {
            Title = "Widget",
            Brand = "Acme",
            Description = "Existing",
            MainImageUrl = "",
            CategoryId = category.Id,
            Quantity = 3,
            RegularPrice = 10m
        });
        await db.SaveChangesAsync();

        var reader = new Mock<IProductExcelWorkbookReader>();
        reader.Setup(x => x.Read(It.IsAny<Stream>()))
            .Returns([
                Row(
                    title: "Widget",
                    category: "Tools",
                    regularPrice: "99",
                    brand: "Acme",
                    quantity: "100")
            ]);

        ProductExcelImportResult result =
            await CreateService(db, reader.Object)
                .ImportAsync(
                    ExcelFile(),
                    updateExisting: false,
                    CancellationToken.None);

        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Updated);
        Assert.Equal(0, result.Created);

        Product product = await db.Products.SingleAsync();
        Assert.Equal(10m, product.RegularPrice);
        Assert.Equal((uint)3, product.Quantity);
        Assert.Equal("skipped", Assert.Single(result.Rows).Status);
    }

    [Fact]
    public async Task ImportAsync_ImportsValidRows_AndReportsUnknownCategory()
    {
        await using ApplicationDbContext db = CreateDb();
        await AddCategoryAsync(db, "Tools");

        var reader = new Mock<IProductExcelWorkbookReader>();
        reader.Setup(x => x.Read(It.IsAny<Stream>()))
            .Returns([
                Row("Broken", "Missing", "10"),
                Row("Valid", "Tools", "15")
            ]);

        ProductExcelImportResult result =
            await CreateService(db, reader.Object)
                .ImportAsync(
                    ExcelFile(),
                    updateExisting: true,
                    CancellationToken.None);

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.Failed);
        Assert.Single(await db.Products.ToListAsync());

        ProductExcelImportRowResult error =
            Assert.Single(
                result.Rows.Where(row => row.Status == "error"));

        Assert.Contains("does not exist", error.Message);
    }

    [Fact]
    public async Task ImportAsync_RejectsDuplicateRowsWithinWorkbook()
    {
        await using ApplicationDbContext db = CreateDb();
        await AddCategoryAsync(db, "Tools");

        var reader = new Mock<IProductExcelWorkbookReader>();
        reader.Setup(x => x.Read(It.IsAny<Stream>()))
            .Returns([
                Row("Widget", "Tools", "10", brand: "Acme"),
                Row("widget", "Tools", "20", brand: "ACME")
            ]);

        ProductExcelImportResult result =
            await CreateService(db, reader.Object)
                .ImportAsync(
                    ExcelFile(),
                    updateExisting: true,
                    CancellationToken.None);

        Assert.Equal(1, result.Created);
        Assert.Equal(1, result.Failed);
        Assert.Single(await db.Products.ToListAsync());
        Assert.Contains(
            result.Rows,
            row =>
                row.Status == "error" &&
                row.Message.Contains("Duplicate product row"));
    }

    [Fact]
    public async Task ImportAsync_ReportsInvalidNumericRow_WithoutPersistingIt()
    {
        await using ApplicationDbContext db = CreateDb();
        await AddCategoryAsync(db, "Tools");

        var reader = new Mock<IProductExcelWorkbookReader>();
        reader.Setup(x => x.Read(It.IsAny<Stream>()))
            .Returns([
                Row("Widget", "Tools", "not-a-number")
            ]);

        ProductExcelImportResult result =
            await CreateService(db, reader.Object)
                .ImportAsync(
                    ExcelFile(),
                    updateExisting: true,
                    CancellationToken.None);

        Assert.Equal(0, result.Created);
        Assert.Equal(1, result.Failed);
        Assert.Empty(await db.Products.ToListAsync());
        Assert.Contains(
            "RegularPrice",
            Assert.Single(result.Rows).Message);
    }

    [Fact]
    public async Task ImportAsync_RejectsNonXlsxFile_BeforeReadingWorkbook()
    {
        await using ApplicationDbContext db = CreateDb();
        var reader = new Mock<IProductExcelWorkbookReader>();

        ProductExcelImportException exception =
            await Assert.ThrowsAsync<ProductExcelImportException>(
                () => CreateService(db, reader.Object)
                    .ImportAsync(
                        ExcelFile("products.xls"),
                        updateExisting: true,
                        CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        reader.Verify(
            x => x.Read(It.IsAny<Stream>()),
            Times.Never);
    }

    private static ProductExcelImportService CreateService(
        ApplicationDbContext db,
        IProductExcelWorkbookReader reader) =>
        new(
            db,
            reader,
            new ProductPricingPolicy());

    private static ApplicationDbContext CreateDb()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"excel-product-import-{Guid.NewGuid():N}")
                .Options;

        return new ApplicationDbContext(options);
    }

    private static async Task<Category> AddCategoryAsync(
        ApplicationDbContext db,
        string name)
    {
        var category = new Category
        {
            Name = name,
            ImageUri = null
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return category;
    }

    private static IFormFile ExcelFile(
        string fileName = "products.xlsx")
    {
        var stream =
            new MemoryStream([1, 2, 3]);

        return new FormFile(
            stream,
            0,
            stream.Length,
            "file",
            fileName);
    }

    private static ProductExcelImportRow Row(
        string title,
        string category,
        string regularPrice,
        string? brand = null,
        string description = "",
        string discountPercentage = "",
        string discountedPrice = "",
        string wholesalePrice = "",
        string wholesaleMinQuantity = "",
        string vatRate = "",
        string quantity = "",
        string mainImageUrl = "",
        string isActive = "") =>
        new(
            2,
            title,
            description,
            brand,
            category,
            regularPrice,
            discountPercentage,
            discountedPrice,
            wholesalePrice,
            wholesaleMinQuantity,
            vatRate,
            quantity,
            mainImageUrl,
            isActive);
}
