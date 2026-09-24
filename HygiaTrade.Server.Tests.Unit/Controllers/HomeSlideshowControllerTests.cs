using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class HomeSlideshowControllerTests
{
    private readonly Mock<IHomeSlideshowService> slideshowService = new();

    private HomeSlideshowController CreateController() =>
        new(slideshowService.Object);

    [Fact]
    public async Task GetAsync_ReturnsOk_WithPayload()
    {
        HomeSlideshowPayload expected = new()
        {
            Slides =
            [
                new HomeSlideDto
                {
                    Id = "slide-1",
                    TitleBg = "Заглавие"
                }
            ]
        };

        slideshowService
            .Setup(service => service.GetAsync())
            .ReturnsAsync(expected);

        ActionResult<HomeSlideshowPayload> result =
            await CreateController().GetAsync();

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result.Result);

        Assert.Same(expected, ok.Value);

        slideshowService.Verify(
            service => service.GetAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_PropagatesUnexpectedException()
    {
        InvalidOperationException expected =
            new("Storage failed.");

        slideshowService
            .Setup(service => service.GetAsync())
            .ThrowsAsync(expected);

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateController().GetAsync());

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsOk_WithUpdatedPayload()
    {
        HomeSlideshowPayload request = new()
        {
            Slides =
            [
                new HomeSlideDto
                {
                    Id = "slide-1",
                    TitleBg = "Old"
                }
            ]
        };

        HomeSlideshowPayload expected = new()
        {
            Slides =
            [
                new HomeSlideDto
                {
                    Id = "slide-1",
                    TitleBg = "Updated"
                }
            ]
        };

        slideshowService
            .Setup(service => service.UpdateAsync(request))
            .ReturnsAsync(expected);

        ActionResult<HomeSlideshowPayload> result =
            await CreateController().UpdateAsync(request);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result.Result);

        Assert.Same(expected, ok.Value);

        slideshowService.Verify(
            service => service.UpdateAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsMappedStatusAndMessage_WhenServiceThrows()
    {
        HomeSlideshowPayload request = new();

        HomeSlideshowServiceException exception =
            new(
                StatusCodes.Status400BadRequest,
                "At least one slideshow item is required.");

        slideshowService
            .Setup(service => service.UpdateAsync(request))
            .ThrowsAsync(exception);

        ActionResult<HomeSlideshowPayload> result =
            await CreateController().UpdateAsync(request);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            error.StatusCode);

        Assert.Equal(
            exception.Message,
            error.Value?
                .GetType()
                .GetProperty("message")
                ?.GetValue(error.Value)
                ?.ToString());
    }
}
