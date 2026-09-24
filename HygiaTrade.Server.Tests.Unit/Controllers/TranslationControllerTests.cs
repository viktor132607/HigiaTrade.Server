using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class TranslationControllerTests
{
    private readonly Mock<ITranslationService> translationService = new();

    private TranslationController CreateController() =>
        new(translationService.Object);

    [Fact]
    public async Task TranslateBgToEnAsync_ReturnsOk_WithTranslation()
    {
        TranslationRequest request = new()
        {
            Text = "Здравей"
        };

        TranslationResponse expected = new()
        {
            Translation = "Hello"
        };

        translationService
            .Setup(service => service.TranslateBgToEnAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        ActionResult<TranslationResponse> result =
            await CreateController().TranslateBgToEnAsync(
                request,
                CancellationToken.None);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result.Result);

        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task TranslateBgToEnAsync_ReturnsMappedStatusAndPayload_WhenServiceThrows()
    {
        TranslationRequest request = new()
        {
            Text = "Здравей"
        };

        object payload = new
        {
            message = "Translation service failed."
        };

        TranslationServiceException exception =
            new(502, payload);

        translationService
            .Setup(service => service.TranslateBgToEnAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        ActionResult<TranslationResponse> result =
            await CreateController().TranslateBgToEnAsync(
                request,
                CancellationToken.None);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(502, error.StatusCode);
        Assert.Same(payload, error.Value);
    }
}
