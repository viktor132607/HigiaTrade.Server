using HygiaTrade.API.Services;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class StoredImageRepositoryTests
{
    [Fact]
    public async Task StoreAsync_PersistsImageAndReturnsEntity()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        var repository =
            new StoredImageRepository(db);

        byte[] data = [1, 2, 3];

        StoredImage result =
            await repository.StoreAsync(
                "photo.png",
                "image/png",
                data,
                CancellationToken.None);

        StoredImage? stored =
            await db.StoredImages
                .SingleOrDefaultAsync(
                    x => x.Id == result.Id);

        Assert.NotNull(stored);
        Assert.Equal("photo.png", stored.FileName);
        Assert.Equal("image/png", stored.ContentType);
        Assert.Equal(data, stored.Data);
    }

    [Fact]
    public async Task GetAsync_ReturnsNonDeletedImage()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        StoredImage image = new()
        {
            FileName = "photo.jpg",
            ContentType = "image/jpeg",
            Data = [4, 5, 6]
        };

        db.StoredImages.Add(image);
        await db.SaveChangesAsync();

        var repository =
            new StoredImageRepository(db);

        StoredImage? result =
            await repository.GetAsync(
                image.Id,
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(image.Id, result.Id);
        Assert.Equal(image.FileName, result.FileName);
        Assert.Equal(image.ContentType, result.ContentType);
        Assert.Equal(image.Data, result.Data);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_ForDeletedImage()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        StoredImage image = new()
        {
            FileName = "deleted.gif",
            ContentType = "image/gif",
            Data = [7],
            IsDeleted = true
        };

        db.StoredImages.Add(image);
        await db.SaveChangesAsync();

        var repository =
            new StoredImageRepository(db);

        StoredImage? result =
            await repository.GetAsync(
                image.Id,
                CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_ForMissingImage()
    {
        await using ApplicationDbContext db =
            CreateDbContext();

        var repository =
            new StoredImageRepository(db);

        StoredImage? result =
            await repository.GetAsync(
                Guid.NewGuid(),
                CancellationToken.None);

        Assert.Null(result);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        DbContextOptions<ApplicationDbContext> options =
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(
                    $"stored-images-{Guid.NewGuid():N}")
                .Options;

        return new ApplicationDbContext(options);
    }
}
