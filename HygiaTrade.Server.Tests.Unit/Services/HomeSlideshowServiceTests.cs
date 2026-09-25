using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class HomeSlideshowServiceTests
{
    private readonly Mock<IHomeSlideshowStore> store = new();
    private readonly Mock<IHomeSlideshowPayloadNormalizer> normalizer = new();
    private readonly Mock<IHomeSlideshowDefaults> defaults = new();

    private HomeSlideshowService CreateService() =>
        new(store.Object, normalizer.Object, defaults.Object);

    [Fact]
    public async Task GetAsync_ReturnsStoredPayload_WhenPresent()
    {
        HomeSlideshowPayload expected = Payload("stored");

        store.Setup(x => x.ReadAsync())
            .ReturnsAsync(expected);

        HomeSlideshowPayload result =
            await CreateService().GetAsync();

        Assert.Same(expected, result);
        store.Verify(x => x.EnsureAsync(), Times.Once);
        defaults.Verify(x => x.Create(), Times.Never);
    }

    [Fact]
    public async Task GetAsync_ReturnsDefaults_WhenStoreHasNoPayload()
    {
        HomeSlideshowPayload expected = Payload("default");

        store.Setup(x => x.ReadAsync())
            .ReturnsAsync((HomeSlideshowPayload?)null);

        defaults.Setup(x => x.Create())
            .Returns(expected);

        HomeSlideshowPayload result =
            await CreateService().GetAsync();

        Assert.Same(expected, result);
        store.Verify(x => x.EnsureAsync(), Times.Once);
        defaults.Verify(x => x.Create(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NormalizesEnsuresWritesAndReturnsSamePayload()
    {
        HomeSlideshowPayload payload = Payload("slide");

        HomeSlideshowPayload result =
            await CreateService().UpdateAsync(payload);

        Assert.Same(payload, result);

        normalizer.Verify(
            x => x.Normalize(payload),
            Times.Once);

        store.Verify(
            x => x.EnsureAsync(),
            Times.Once);

        store.Verify(
            x => x.WriteAsync(payload),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotTouchStore_WhenNormalizationFails()
    {
        HomeSlideshowPayload payload = Payload("slide");

        normalizer
            .Setup(x => x.Normalize(payload))
            .Throws(new HomeSlideshowServiceException(
                400,
                "bad"));

        await Assert.ThrowsAsync<HomeSlideshowServiceException>(
            () => CreateService().UpdateAsync(payload));

        store.Verify(
            x => x.EnsureAsync(),
            Times.Never);

        store.Verify(
            x => x.WriteAsync(
                It.IsAny<HomeSlideshowPayload>()),
            Times.Never);
    }

    private static HomeSlideshowPayload Payload(
        string id) =>
        new()
        {
            Slides =
            [
                new HomeSlideDto
                {
                    Id = id
                }
            ]
        };
}
