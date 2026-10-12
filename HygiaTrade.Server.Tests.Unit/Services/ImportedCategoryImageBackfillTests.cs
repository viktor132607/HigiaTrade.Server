using HygiaTrade.API.Services;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class ImportedCategoryImageBackfillTests
{
    private static ApplicationDbContext CreateDb() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Apply_FillsEightMissingMainCategoryImagesAndIsIdempotent()
    {
        await using var db = CreateDb();
        string[] categories =
        [
            "Аксесоари", "Ароматизатори за въздух", "Инсектициди",
            "Перилни препарати", "Почистващи препарати",
            "Препарат за петна", "Препарати за миене на съдове",
            "Професионално почистване"
        ];
        foreach (string name in categories)
            db.Categories.Add(new Category { Name = name, ImageUri = null });
        await db.SaveChangesAsync();

        Assert.Equal(8, await ImportedCategoryImageBackfill.ApplyAsync(db));
        Assert.All(await db.Categories.ToListAsync(), item =>
            Assert.StartsWith("https://higiatrade.com/images/", item.ImageUri));
        Assert.Equal(0, await ImportedCategoryImageBackfill.ApplyAsync(db));
    }

    [Fact]
    public async Task Apply_PreservesExistingArtworkAndSubcategoryPictures()
    {
        await using var db = CreateDb();
        var parent = new Category
        {
            Name = "Аксесоари",
            ImageUri = "https://example.org/my-custom-image.jpg"
        };
        db.Categories.Add(parent);
        db.Categories.Add(new Category
        {
            Name = "Инсектициди",
            ImageUri = null,
            ParentCategoryId = parent.Id
        });
        await db.SaveChangesAsync();

        Assert.Equal(0, await ImportedCategoryImageBackfill.ApplyAsync(db));
        Assert.Equal("https://example.org/my-custom-image.jpg",
            (await db.Categories.SingleAsync(x => x.Id == parent.Id)).ImageUri);
        Assert.Null((await db.Categories.SingleAsync(x => x.ParentCategoryId != null)).ImageUri);
    }
}
