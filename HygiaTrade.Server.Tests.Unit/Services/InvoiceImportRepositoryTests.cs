using HygiaTrade.API.Services;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class InvoiceImportRepositoryTests
{
    [Fact]
    public async Task GetCatalogAsync_ReturnsNonDeletedProductsOrderedByTitle()
    {
        await using ApplicationDbContext db = CreateDb();

        Guid alphaId = Guid.NewGuid();
        Guid betaId = Guid.NewGuid();

        db.Products.AddRange(
            Product(alphaId, "Alpha"),
            Product(betaId, "Beta"),
            Product(Guid.NewGuid(), "Deleted", true));

        await db.SaveChangesAsync();

        var repository = new InvoiceImportRepository(db);

        IReadOnlyList<InvoiceCatalogProduct> result =
            await repository.GetCatalogAsync(
                CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(alphaId, result[0].Id);
        Assert.Equal("Alpha", result[0].Title);
        Assert.Equal(betaId, result[1].Id);
    }

    [Fact]
    public async Task GetProductsAsync_ReturnsRequestedNonDeletedProducts()
    {
        await using ApplicationDbContext db = CreateDb();

        Guid wantedId = Guid.NewGuid();
        Guid deletedId = Guid.NewGuid();

        db.Products.AddRange(
            Product(wantedId, "Wanted"),
            Product(deletedId, "Deleted", true),
            Product(Guid.NewGuid(), "Other"));

        await db.SaveChangesAsync();

        var repository = new InvoiceImportRepository(db);

        IReadOnlyList<Product> result =
            await repository.GetProductsAsync(
                [wantedId, deletedId],
                CancellationToken.None);

        Product item = Assert.Single(result);
        Assert.Equal(wantedId, item.Id);
    }

    private static Product Product(
        Guid id,
        string title,
        bool deleted = false) =>
        new()
        {
            Id = id,
            Title = title,
            Description = string.Empty,
            MainImageUrl = string.Empty,
            IsDeleted = deleted
        };

    private static ApplicationDbContext CreateDb()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"InvoiceImportRepository-{Guid.NewGuid():N}")
                .Options;

        return new ApplicationDbContext(options);
    }
}
