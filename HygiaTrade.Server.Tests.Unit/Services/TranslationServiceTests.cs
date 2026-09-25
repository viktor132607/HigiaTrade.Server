using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class TranslationServiceTests
{
    private readonly Mock<ITranslationRequestPolicy> policy = new();
    private readonly Mock<IDeepLTranslationGateway> gateway = new();

    private TranslationService CreateService() =>
        new(policy.Object, gateway.Object);

    [Fact]
    public async Task TranslateBgToEnAsync_ReturnsEmptyWithoutCallingProvider()
    {
        var request = new TranslationRequest
        {
            Text = "   "
        };

        policy
            .Setup(x => x.Normalize(request.Text))
            .Returns(string.Empty);

        TranslationResponse result =
            await CreateService().TranslateBgToEnAsync(
                request,
                CancellationToken.None);

        Assert.Equal(string.Empty, result.Translation);

        gateway.Verify(
            x => x.TranslateBgToEnAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TranslateBgToEnAsync_DelegatesNormalizedTextToGateway()
    {
        var request = new TranslationRequest
        {
            Text = "  Здравей  "
        };

        policy
            .Setup(x => x.Normalize(request.Text))
            .Returns("Здравей");

        gateway
            .Setup(x => x.TranslateBgToEnAsync(
                "Здравей",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello");

        TranslationResponse result =
            await CreateService().TranslateBgToEnAsync(
                request,
                CancellationToken.None);

        Assert.Equal("Hello", result.Translation);
    }
}
