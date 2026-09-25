using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public interface IStoredImageRepository
{
    Task<StoredImage> StoreAsync(
        string fileName,
        string contentType,
        byte[] data,
        CancellationToken cancellationToken);

    Task<StoredImage?> GetAsync(
        Guid id,
        CancellationToken cancellationToken);
}

public sealed class StoredImageRepository(
    ApplicationDbContext db)
    : IStoredImageRepository
{
    public async Task<StoredImage> StoreAsync(
        string fileName,
        string contentType,
        byte[] data,
        CancellationToken cancellationToken)
    {
        StoredImage storedImage = new()
        {
            FileName = fileName,
            ContentType = contentType,
            Data = data
        };

        db.StoredImages.Add(storedImage);

        await db.SaveChangesAsync(
            cancellationToken);

        return storedImage;
    }

    public Task<StoredImage?> GetAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        db.StoredImages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.Id == id &&
                    !item.IsDeleted,
                cancellationToken);
}
