using HygiaTrade.API.Services;
using HygiaTrade.Core.Enums;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class ReportsDataSourceTests
{
    [Fact]
    public async Task DataSource_FiltersAndProjectsReportData()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        Category category =
            new()
            {
                Name = "Cleaning",
                ImageUri = null
            };

        Category deletedCategory =
            new()
            {
                Name = "Deleted",
                ImageUri = null
            };

        Product active =
            ProductFor(
                "Active",
                5,
                category);

        Product deleted =
            ProductFor(
                "Deleted Product",
                9,
                deletedCategory);

        deleted.IsDeleted = true;

        DateTime inside =
            new(
                2026,
                1,
                15,
                12,
                0,
                0,
                DateTimeKind.Utc);

        Order validOrder =
            new()
            {
                CreatedOn = inside,
                Status = OrderStatus.Delivered,
                OrderTotalPrice = 25m
            };

        Order cancelledOrder =
            new()
            {
                CreatedOn = inside,
                Status = OrderStatus.Cancelled,
                OrderTotalPrice = 30m
            };

        Order outsideOrder =
            new()
            {
                CreatedOn = new DateTime(
                    2025,
                    12,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc),
                Status = OrderStatus.Delivered,
                OrderTotalPrice = 40m
            };

        Order deletedOrder =
            new()
            {
                CreatedOn = inside,
                Status = OrderStatus.Delivered,
                OrderTotalPrice = 50m,
                IsDeleted = true
            };

        OrderItem validItem =
            ItemFor(
                validOrder,
                active,
                2,
                20m,
                "Active");

        OrderItem cancelledItem =
            ItemFor(
                cancelledOrder,
                active,
                3,
                30m,
                "Cancelled");

        OrderItem deletedItem =
            ItemFor(
                validOrder,
                active,
                1,
                5m,
                "Deleted Item");

        deletedItem.IsDeleted = true;

        db.AddRange(
            category,
            deletedCategory,
            active,
            deleted,
            validOrder,
            cancelledOrder,
            outsideOrder,
            deletedOrder,
            validItem,
            cancelledItem,
            deletedItem);

        await db.SaveChangesAsync();

        var source = new ReportsDataSource(db);

        IReadOnlyList<ReportProductData> products =
            await source.GetProductsAsync(
                CancellationToken.None);

        Assert.Single(products);
        Assert.Equal(active.Id, products[0].Id);
        Assert.Equal("Cleaning", products[0].CategoryName);
        Assert.Equal(5u, products[0].Quantity);

        IReadOnlyList<ReportOrderData> orders =
            await source.GetOrdersAsync(
                new DateTime(
                    2026,
                    1,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc),
                new DateTime(
                    2026,
                    2,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc),
                CancellationToken.None);

        Assert.Single(orders);
        Assert.Equal(validOrder.Id, orders[0].Id);
        Assert.Equal(25m, orders[0].OrderTotalPrice);

        IReadOnlyList<ReportOrderItemData> items =
            await source.GetOrderItemsAsync(
                [validOrder.Id],
                CancellationToken.None);

        Assert.Single(items);
        Assert.Equal(validItem.ProductId, items[0].ProductId);
        Assert.Equal(2, items[0].Quantity);
        Assert.Equal(20m, items[0].TotalPrice);
        Assert.Equal(validOrder.Id, items[0].OrderId);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"reports-{Guid.NewGuid():N}")
                .Options;

        return new ApplicationDbContext(options);
    }

    private static Product ProductFor(
        string title,
        uint quantity,
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
            Quantity = quantity,
            CategoryId = category.Id,
            Category = category
        };

    private static OrderItem ItemFor(
        Order order,
        Product product,
        int quantity,
        decimal total,
        string title) =>
        new()
        {
            OrderId = order.Id,
            Order = order,
            ProductId = product.Id,
            Product = product,
            Quantity = quantity,
            SinglePrice = total / quantity,
            TotalPrice = total,
            Title = title,
            PrimaryImageUri = "image"
        };
}
