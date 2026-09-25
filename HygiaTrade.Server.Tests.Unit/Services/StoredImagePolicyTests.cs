using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class StoredImagePolicyTests
{
    private readonly StoredImagePolicy policy = new();

    [Fact]
    public void Validate_RejectsEmptyFile()
    {
        IFormFile file =
            File(
                0,
                "image/png",
                "photo.png");

        StoredImageServiceException ex =
            Assert.Throws<StoredImageServiceException>(
                () => policy.Validate(file));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "Choose an image to upload.",
            ex.Message);
    }

    [Fact]
    public void Validate_RejectsFileOverTenMegabytes()
    {
        IFormFile file =
            File(
                10L * 1024 * 1024 + 1,
                "image/png",
                "photo.png");

        StoredImageServiceException ex =
            Assert.Throws<StoredImageServiceException>(
                () => policy.Validate(file));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "Image size cannot exceed 10 MB.",
            ex.Message);
    }

    [Theory]
    [InlineData("image/jpeg", "image/jpeg")]
    [InlineData("IMAGE/JPEG", "image/jpeg")]
    [InlineData("image/png", "image/png")]
    [InlineData("image/webp", "image/webp")]
    [InlineData("image/gif", "image/gif")]
    public void Validate_AcceptsSupportedContentTypesAndNormalizesCase(
        string inputContentType,
        string expectedContentType)
    {
        IFormFile file =
            File(
                1,
                inputContentType,
                "photo.png");

        StoredImageUploadDescriptor result =
            policy.Validate(file);

        Assert.Equal(
            expectedContentType,
            result.ContentType);
    }

    [Theory]
    [InlineData("image/svg+xml")]
    [InlineData("application/pdf")]
    [InlineData("")]
    public void Validate_RejectsUnsupportedContentTypes(
        string contentType)
    {
        IFormFile file =
            File(
                1,
                contentType,
                "photo.bin");

        StoredImageServiceException ex =
            Assert.Throws<StoredImageServiceException>(
                () => policy.Validate(file));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "Only JPEG, PNG, WEBP and GIF images are supported.",
            ex.Message);
    }

    [Fact]
    public void Validate_AcceptsExactlyTenMegabytesAndSanitizesFileName()
    {
        IFormFile file =
            File(
                10L * 1024 * 1024,
                "image/png",
                "../folder/photo.png");

        StoredImageUploadDescriptor result =
            policy.Validate(file);

        Assert.Equal(
            "photo.png",
            result.FileName);

        Assert.Equal(
            "image/png",
            result.ContentType);
    }

    private static IFormFile File(
        long length,
        string contentType,
        string fileName)
    {
        var file =
            new Mock<IFormFile>();

        file.SetupGet(x => x.Length)
            .Returns(length);

        file.SetupGet(x => x.ContentType)
            .Returns(contentType);

        file.SetupGet(x => x.FileName)
            .Returns(fileName);

        return file.Object;
    }
}
