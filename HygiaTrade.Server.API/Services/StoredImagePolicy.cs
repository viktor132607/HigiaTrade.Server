namespace HygiaTrade.API.Services;

public sealed record StoredImageUploadDescriptor(
    string FileName,
    string ContentType);

public interface IStoredImagePolicy
{
    StoredImageUploadDescriptor Validate(
        IFormFile file);
}

public sealed class StoredImagePolicy
    : IStoredImagePolicy
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

    public StoredImageUploadDescriptor Validate(
        IFormFile file)
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

        if (!AllowedContentTypes.Contains(
                contentType))
        {
            throw new StoredImageServiceException(
                StatusCodes.Status400BadRequest,
                "Only JPEG, PNG, WEBP and GIF images are supported.");
        }

        return new StoredImageUploadDescriptor(
            Path.GetFileName(file.FileName),
            contentType);
    }
}
