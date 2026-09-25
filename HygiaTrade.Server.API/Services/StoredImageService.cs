using HygiaTrade.Data.Entities;

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
    IStoredImagePolicy policy,
    IStoredImageRepository repository)
    : IStoredImageService
{
    public async Task<StoredImageUploadResult> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        StoredImageUploadDescriptor descriptor =
            policy.Validate(file);

        await using MemoryStream stream = new();

        await file.CopyToAsync(
            stream,
            cancellationToken);

        byte[] data = stream.ToArray();

        StoredImage storedImage =
            await repository.StoreAsync(
                descriptor.FileName,
                descriptor.ContentType,
                data,
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
        StoredImage? image =
            await repository.GetAsync(
                id,
                cancellationToken);

        return image is null
            ? null
            : new StoredImageDownloadResult(
                image.Data,
                image.ContentType);
    }
}
