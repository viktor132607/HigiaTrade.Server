using HygiaTrade.API.Services;
using HygiaTrade.Data.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class StoredImageServiceTests
{
    private readonly Mock<IStoredImagePolicy> policy = new();
    private readonly Mock<IStoredImageRepository> repository = new();

    private StoredImageService CreateService() =>
        new(
            policy.Object,
            repository.Object);

    [Fact]
    public async Task UploadAsync_ValidatesCopiesStoresAndMapsResult()
    {
        byte[] bytes = [1, 2, 3, 4];

        await using MemoryStream stream =
            new(bytes);

        IFormFile file =
            new FormFile(
                stream,
                0,
                bytes.Length,
                "file",
                "photo.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

        StoredImageUploadDescriptor descriptor =
            new(
                "photo.png",
                "image/png");

        policy
            .Setup(x => x.Validate(file))
            .Returns(descriptor);

        StoredImage stored = new()
        {
            FileName = descriptor.FileName,
            ContentType = descriptor.ContentType,
            Data = bytes
        };

        repository
            .Setup(x => x.StoreAsync(
                descriptor.FileName,
                descriptor.ContentType,
                It.Is<byte[]>(data =>
                    data.SequenceEqual(bytes)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        StoredImageUploadResult result =
            await CreateService().UploadAsync(
                file,
                CancellationToken.None);

        Assert.Equal(stored.Id, result.Id);
        Assert.Equal("photo.png", result.FileName);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal(bytes.Length, result.Size);
    }

    [Fact]
    public async Task UploadAsync_DoesNotReadOrStore_WhenPolicyRejectsFile()
    {
        var file =
            new Mock<IFormFile>();

        StoredImageServiceException expected =
            new(
                400,
                "Choose an image to upload.");

        policy
            .Setup(x => x.Validate(file.Object))
            .Throws(expected);

        StoredImageServiceException actual =
            await Assert.ThrowsAsync<StoredImageServiceException>(
                () => CreateService().UploadAsync(
                    file.Object,
                    CancellationToken.None));

        Assert.Same(expected, actual);

        file.Verify(
            x => x.CopyToAsync(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        repository.Verify(
            x => x.StoreAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenRepositoryDoesNotFindImage()
    {
        Guid id = Guid.NewGuid();

        repository
            .Setup(x => x.GetAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredImage?)null);

        StoredImageDownloadResult? result =
            await CreateService().GetAsync(
                id,
                CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_MapsStoredImageData()
    {
        Guid id = Guid.NewGuid();

        StoredImage stored = new()
        {
            Id = id,
            FileName = "photo.webp",
            ContentType = "image/webp",
            Data = [9, 8, 7]
        };

        repository
            .Setup(x => x.GetAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        StoredImageDownloadResult? result =
            await CreateService().GetAsync(
                id,
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Same(stored.Data, result.Data);
        Assert.Equal(
            "image/webp",
            result.ContentType);
    }
}
