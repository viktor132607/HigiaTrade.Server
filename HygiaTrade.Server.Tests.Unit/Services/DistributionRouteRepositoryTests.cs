using HygiaTrade.API.Services;
using HygiaTrade.Core.Enums;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class DistributionRouteRepositoryTests
{
    [Fact]
    public async Task GetActiveOrdersAsync_FiltersAndOrdersResults()
    {
        await using ApplicationDbContext db = CreateDb();

        Guid firstId = Guid.NewGuid();
        Guid secondId = Guid.NewGuid();

        db.Orders.AddRange(
            new Order
            {
                Id = secondId,
                Status = OrderStatus.Processing,
                CreatedOn = new DateTime(2026, 9, 2)
            },
            new Order
            {
                Id = firstId,
                Status = OrderStatus.Created,
                CreatedOn = new DateTime(2026, 9, 1)
            },
            new Order
            {
                Status = OrderStatus.Delivered
            },
            new Order
            {
                Status = OrderStatus.Cancelled
            },
            new Order
            {
                Status = OrderStatus.Processing,
                IsDeleted = true
            });

        await db.SaveChangesAsync();

        var repository =
            new DistributionRouteRepository(db);

        IReadOnlyList<Order> result =
            await repository.GetActiveOrdersAsync(
                CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(firstId, result[0].Id);
        Assert.Equal(secondId, result[1].Id);
    }

    [Fact]
    public async Task GetOrdersAsync_ReturnsRequestedNonDeletedOrders()
    {
        await using ApplicationDbContext db = CreateDb();

        Guid wanted = Guid.NewGuid();
        Guid deleted = Guid.NewGuid();
        Guid other = Guid.NewGuid();

        db.Orders.AddRange(
            new Order
            {
                Id = wanted
            },
            new Order
            {
                Id = deleted,
                IsDeleted = true
            },
            new Order
            {
                Id = other
            });

        await db.SaveChangesAsync();

        var repository =
            new DistributionRouteRepository(db);

        IReadOnlyList<Order> result =
            await repository.GetOrdersAsync(
                [wanted, deleted],
                CancellationToken.None);

        Order item = Assert.Single(result);
        Assert.Equal(wanted, item.Id);
    }

    private static ApplicationDbContext CreateDb()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"DistributionRouteRepository-{Guid.NewGuid():N}")
                .Options;

        return new ApplicationDbContext(options);
    }
}
