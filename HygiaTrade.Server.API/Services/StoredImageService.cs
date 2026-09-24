using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Services;

public interface IStoredImageService
{
    Task<StoredImageUploadResult> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken);

    Task<StoredImageDownloadResult?> GetAsync(
        Guid id,
        CancellationToken cancellationToken);
}

public sealed record StoredImageUploadResult(
    Guid Id,
    string FileName,
    string ContentType,
    int Size);

public sealed record StoredImageDownloadResult(
    byte[] Data,
    string ContentType);

public sealed class StoredImageServiceException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class StoredImageService(
    ApplicationDbContext db) : IStoredImageService
{
    private const long MaxImageSize =
        10 * 1024 * 1024;

    private static readonly HashSet<string>
        AllowedContentTypes =
        [
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/gif"
        ];

    public async Task<StoredImageUploadResult> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new StoredImageServiceException(
                StatusCodes.Status400BadRequest,
                "Choose an image to upload.");
        }

        if (file.Length > MaxImageSize)
        {
            throw new StoredImageServiceException(
                StatusCodes.Status400BadRequest,
                "Image size cannot exceed 10 MB.");
        }

        string contentType =
            file.ContentType.ToLowerInvariant();

        if (!AllowedContentTypes.Contains(contentType))
        {
            throw new StoredImageServiceException(
                StatusCodes.Status400BadRequest,
                "Only JPEG, PNG, WEBP and GIF images are supported.");
        }

        await using MemoryStream stream = new();

        await file.CopyToAsync(
            stream,
            cancellationToken);

        StoredImage storedImage = new()
        {
            FileName =
                Path.GetFileName(file.FileName),

            ContentType =
                contentType,

            Data =
                stream.ToArray()
        };

        db.StoredImages.Add(storedImage);

        await db.SaveChangesAsync(
            cancellationToken);

        return new StoredImageUploadResult(
            storedImage.Id,
            storedImage.FileName,
            storedImage.ContentType,
            storedImage.Data.Length);
    }

    public async Task<StoredImageDownloadResult?> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        StoredImage? image = await db.StoredImages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.Id == id &&
                    !item.IsDeleted,
                cancellationToken);

        return image is null
            ? null
            : new StoredImageDownloadResult(
                image.Data,
                image.ContentType);
    }
}
