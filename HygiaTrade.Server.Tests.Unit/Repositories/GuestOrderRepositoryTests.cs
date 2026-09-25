using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Tests.Unit.Repositories;

public sealed class GuestOrderRepositoryTests
{
    [Fact]
    public async Task GetAvailableProductsAsync_FiltersInactiveAndDeleted()
    {
        await using ApplicationDbContext db = CreateDb();

        Guid availableId = Guid.NewGuid();

        db.Products.AddRange(
            Product(availableId, true, false),
            Product(Guid.NewGuid(), false, false),
            Product(Guid.NewGuid(), true, true));

        await db.SaveChangesAsync();

        var repository = new GuestOrderRepository(db);

        Dictionary<Guid, Product> result =
            await repository.GetAvailableProductsAsync(
                db.Products.Select(product => product.Id).ToArray());

        Product available = Assert.Single(result).Value;
        Assert.Equal(availableId, available.Id);
    }

    [Fact]
    public async Task SaveAsync_PersistsOrderAndUpdatedProduct()
    {
        await using ApplicationDbContext db = CreateDb();

        Product product =
            Product(Guid.NewGuid(), true, false);

        db.Products.Add(product);
        await db.SaveChangesAsync();

        product.Quantity = 3;

        var order = new Order
        {
            GuestEmail = "guest@example.com"
        };

        var repository = new GuestOrderRepository(db);

        Guid id =
            await repository.SaveAsync(
                order,
                [product]);

        Assert.Equal(order.Id, id);

        Order? saved =
            await db.Orders.FindAsync(order.Id);

        Product? updated =
            await db.Products.FindAsync(product.Id);

        Assert.NotNull(saved);
        Assert.Equal((uint)3, updated!.Quantity);
    }

    private static Product Product(
        Guid id,
        bool active,
        bool deleted) =>
        new()
        {
            Id = id,
            Title = "P",
            Description = string.Empty,
            MainImageUrl = "p",
            IsActive = active,
            IsDeleted = deleted,
            Quantity = 5
        };

    private static ApplicationDbContext CreateDb()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"GuestOrderRepository-{Guid.NewGuid():N}")
                .Options;

        return new ApplicationDbContext(options);
    }
}
