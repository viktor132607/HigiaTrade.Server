using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class InventoryControllerTests
{
    private readonly Mock<IInventoryService> inventoryService = new();

    private InventoryController CreateController() =>
        new(inventoryService.Object);

    [Fact]
    public async Task GetAsync_ReturnsOk_WithInventory()
    {
        Guid productId = Guid.NewGuid();

        InventoryResponse expected =
            new(
                12,
                [
                    new StockEntryResponse(
                        Guid.NewGuid(),
                        5,
                        "INV-001",
                        DateTime.UtcNow)
                ]);

        inventoryService
            .Setup(service => service.GetAsync(productId))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().GetAsync(productId);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetAsync_ReturnsMappedError_WhenServiceThrows()
    {
        Guid productId = Guid.NewGuid();

        InventoryServiceException exception =
            new(404, "Product not found.");

        inventoryService
            .Setup(service => service.GetAsync(productId))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().GetAsync(productId);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(404, error.StatusCode);

        Assert.Equal(
            exception.Message,
            GetProperty(error.Value, "message"));
    }

    [Fact]
    public async Task AddAsync_ReturnsOk_WithUpdatedInventory()
    {
        Guid productId = Guid.NewGuid();

        AddStockRequest request =
            new(3, "INV-002");

        StockEntryResponse entry =
            new(
                Guid.NewGuid(),
                3,
                "INV-002",
                DateTime.UtcNow);

        AddStockResponse expected =
            new(15, entry);

        inventoryService
            .Setup(service => service.AddAsync(
                productId,
                request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().AddAsync(
                productId,
                request);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task AddAsync_ReturnsMappedError_WhenServiceThrows()
    {
        Guid productId = Guid.NewGuid();

        AddStockRequest request =
            new(0, "INV-002");

        InventoryServiceException exception =
            new(
                400,
                "Quantity must be greater than zero.");

        inventoryService
            .Setup(service => service.AddAsync(
                productId,
                request))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().AddAsync(
                productId,
                request);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(400, error.StatusCode);

        Assert.Equal(
            exception.Message,
            GetProperty(error.Value, "message"));
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
