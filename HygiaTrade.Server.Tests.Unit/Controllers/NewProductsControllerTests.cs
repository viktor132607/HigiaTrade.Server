using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class NewProductsControllerTests
{
    private readonly Mock<INewProductsService> newProductsService = new();

    private NewProductsController CreateController() =>
        new(newProductsService.Object);

    [Fact]
    public async Task GetAsync_ReturnsOk_WithPage()
    {
        NewProductsPageResponse expected =
            new([], 0);

        newProductsService
            .Setup(service => service.GetAsync(2, 25))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().GetAsync(2, 25);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsOk_WithStatus()
    {
        Guid productId = Guid.NewGuid();
        NewProductStatusDto expected =
            new(true, 14, DateTime.UtcNow.AddDays(14), true);

        newProductsService
            .Setup(service => service.GetStatusAsync(productId))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().GetStatusAsync(productId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task UpdateStatusAsync_ReturnsOk_WithUpdatedStatus()
    {
        Guid productId = Guid.NewGuid();
        UpdateNewProductStatusRequest request = new(true, 30);
        NewProductStatusDto expected =
            new(true, 30, DateTime.UtcNow.AddDays(30), true);

        newProductsService
            .Setup(service => service.UpdateStatusAsync(productId, request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().UpdateStatusAsync(productId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task UpdateStatusAsync_ReturnsMappedError_WhenServiceThrows()
    {
        Guid productId = Guid.NewGuid();
        UpdateNewProductStatusRequest request = new(true, 500);
        NewProductsServiceException exception =
            new(400, "DisplayDays must be between 1 and 365.");

        newProductsService
            .Setup(service => service.UpdateStatusAsync(productId, request))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().UpdateStatusAsync(productId, request);

        ObjectResult error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, error.StatusCode);
        Assert.Equal(
            exception.Message,
            error.Value?.GetType().GetProperty("message")?.GetValue(error.Value));
    }
}
