using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class ImagesControllerTests
{
    private readonly Mock<IStoredImageService> imageService = new();

    private ImagesController CreateController()
    {
        ImagesController controller = new(imageService.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.Request.Scheme = "https";
        controller.Request.Host = new HostString("example.com");

        return controller;
    }

    [Fact]
    public async Task UploadAsync_ReturnsOk_WithImageMetadataAndUrl()
    {
        IFormFile file = CreateFile([1, 2, 3], "image.png", "image/png");

        StoredImageUploadResult uploaded =
            new(Guid.NewGuid(), "image.png", "image/png", 3);

        imageService
            .Setup(service => service.UploadAsync(
                file,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploaded);

        IActionResult result =
            await CreateController().UploadAsync(
                file,
                CancellationToken.None);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            uploaded.Id,
            GetProperty(ok.Value, "id"));

        Assert.Equal(
            $"https://example.com/api/Images/{uploaded.Id}",
            GetProperty(ok.Value, "url"));

        Assert.Equal(
            uploaded.FileName,
            GetProperty(ok.Value, "fileName"));

        Assert.Equal(
            uploaded.ContentType,
            GetProperty(ok.Value, "contentType"));

        Assert.Equal(
            uploaded.Size,
            GetProperty(ok.Value, "size"));
    }

    [Fact]
    public async Task UploadAsync_ReturnsMappedError_WhenServiceThrows()
    {
        IFormFile file =
            CreateFile([1], "bad.txt", "text/plain");

        StoredImageServiceException exception =
            new(
                StatusCodes.Status400BadRequest,
                "Only JPEG, PNG, WEBP and GIF images are supported.");

        imageService
            .Setup(service => service.UploadAsync(
                file,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().UploadAsync(
                file,
                CancellationToken.None);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            error.StatusCode);

        Assert.Equal(
            exception.Message,
            GetProperty(error.Value, "message"));
    }

    [Fact]
    public async Task GetAsync_ReturnsNotFound_WhenImageDoesNotExist()
    {
        Guid id = Guid.NewGuid();

        imageService
            .Setup(service => service.GetAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StoredImageDownloadResult?)null);

        IActionResult result =
            await CreateController().GetAsync(
                id,
                CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAsync_ReturnsFile_WhenImageExists()
    {
        Guid id = Guid.NewGuid();
        byte[] data = [1, 2, 3];

        StoredImageDownloadResult image =
            new(data, "image/png");

        imageService
            .Setup(service => service.GetAsync(
                id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        IActionResult result =
            await CreateController().GetAsync(
                id,
                CancellationToken.None);

        FileContentResult file =
            Assert.IsType<FileContentResult>(result);

        Assert.Same(data, file.FileContents);
        Assert.Equal("image/png", file.ContentType);
    }

    private static IFormFile CreateFile(
        byte[] data,
        string fileName,
        string contentType)
    {
        MemoryStream stream = new(data);

        return new FormFile(
            stream,
            0,
            data.Length,
            "file",
            fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static object? GetProperty(
        object? value,
        string name)
    {
        Assert.NotNull(value);

        return value
            .GetType()
            .GetProperty(name)
            ?.GetValue(value);
    }
}
