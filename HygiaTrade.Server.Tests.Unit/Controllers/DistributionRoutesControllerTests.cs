using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class DistributionRoutesControllerTests
{
    private readonly Mock<IDistributionRouteService> routeService = new();

    private DistributionRoutesController CreateController() =>
        new(routeService.Object);

    [Fact]
    public async Task GetAsync_ReturnsOk_WithServiceResponse()
    {
        DistributionRoutesPageResponse expected = new()
        {
            Depot = new DistributionDepotDto
            {
                Name = "Depot",
                Address = "Ruse"
            }
        };

        routeService
            .Setup(service => service.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        ActionResult<DistributionRoutesPageResponse> result =
            await CreateController().GetAsync(CancellationToken.None);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result.Result);

        Assert.Same(expected, ok.Value);

        routeService.Verify(
            service => service.GetAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OptimizeAsync_ReturnsOk_WithOptimizedRoute()
    {
        CreateDistributionRouteRequest request = new()
        {
            DistributorName = "Distributor",
            OrderIds = [Guid.NewGuid()]
        };

        DistributionRouteDto expected = new()
        {
            Id = Guid.NewGuid(),
            DistributorName = "Distributor"
        };

        routeService
            .Setup(service => service.OptimizeAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        ActionResult<DistributionRouteDto> result =
            await CreateController().OptimizeAsync(
                request,
                CancellationToken.None);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result.Result);

        Assert.Same(expected, ok.Value);

        routeService.Verify(
            service => service.OptimizeAsync(
                request,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OptimizeAsync_ReturnsMappedStatusAndPayload_WhenServiceThrows()
    {
        CreateDistributionRouteRequest request = new();
        object payload = new
        {
            message = "Invalid route."
        };

        DistributionRouteServiceException exception =
            new(422, payload);

        routeService
            .Setup(service => service.OptimizeAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        ActionResult<DistributionRouteDto> result =
            await CreateController().OptimizeAsync(
                request,
                CancellationToken.None);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result.Result);

        Assert.Equal(422, error.StatusCode);
        Assert.Same(payload, error.Value);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNoContent_WhenServiceSucceeds()
    {
        Guid routeId = Guid.NewGuid();

        routeService
            .Setup(service => service.DeleteAsync(
                routeId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        IActionResult result =
            await CreateController().DeleteAsync(
                routeId,
                CancellationToken.None);

        Assert.IsType<NoContentResult>(result);

        routeService.Verify(
            service => service.DeleteAsync(
                routeId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsMappedStatusAndPayload_WhenServiceThrows()
    {
        Guid routeId = Guid.NewGuid();
        object payload = new
        {
            message = "Route not found."
        };

        DistributionRouteServiceException exception =
            new(404, payload);

        routeService
            .Setup(service => service.DeleteAsync(
                routeId,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().DeleteAsync(
                routeId,
                CancellationToken.None);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(404, error.StatusCode);
        Assert.Same(payload, error.Value);
    }
}
