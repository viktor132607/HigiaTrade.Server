using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class InventoryServiceTests
{
    private readonly Mock<IInventoryReader> reader = new();
    private readonly Mock<IInventoryRequestValidator> validator = new();
    private readonly Mock<IInventoryStockAdder> stockAdder = new();

    private InventoryService CreateService() =>
        new(
            reader.Object,
            validator.Object,
            stockAdder.Object);

    [Fact]
    public async Task GetAsync_ReturnsQuantityAndEntries()
    {
        Guid productId = Guid.NewGuid();

        IReadOnlyList<StockEntryResponse> entries =
        [
            new(
                Guid.NewGuid(),
                5,
                "INV-001",
                DateTime.UtcNow)
        ];

        reader
            .Setup(x => x.GetProductQuantityAsync(productId))
            .ReturnsAsync(12u);

        reader
            .Setup(x => x.GetEntriesAsync(productId))
            .ReturnsAsync(entries);

        InventoryResponse result =
            await CreateService().GetAsync(productId);

        Assert.Equal(12u, result.CurrentQuantity);
        Assert.Same(entries, result.Entries);
    }

    [Fact]
    public async Task GetAsync_Throws404_WhenProductDoesNotExist()
    {
        Guid productId = Guid.NewGuid();

        reader
            .Setup(x => x.GetProductQuantityAsync(productId))
            .ReturnsAsync((uint?)null);

        InventoryServiceException ex =
            await Assert.ThrowsAsync<InventoryServiceException>(
                () => CreateService().GetAsync(productId));

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("Product not found.", ex.Message);

        reader.Verify(
            x => x.GetEntriesAsync(
                It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task AddAsync_NormalizesDelegatesAndMapsResult()
    {
        Guid productId = Guid.NewGuid();
        var request = new AddStockRequest(
            5,
            "  INV-001  ");

        var normalized =
            new InventoryStockRequest(
                5,
                "INV-001");

        DateTime createdOn =
            new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);

        StockEntryResponse entry =
            new(
                Guid.NewGuid(),
                5,
                "INV-001",
                createdOn);

        var adjustment =
            new InventoryStockAdjustment(
                15,
                entry);

        validator
            .Setup(x => x.Normalize(request))
            .Returns(normalized);

        stockAdder
            .Setup(x => x.AddAsync(
                productId,
                normalized))
            .ReturnsAsync(adjustment);

        AddStockResponse result =
            await CreateService().AddAsync(
                productId,
                request);

        Assert.Equal(15u, result.CurrentQuantity);
        Assert.Same(entry, result.Entry);
    }

    [Fact]
    public async Task AddAsync_DoesNotCallStockAdder_WhenValidationFails()
    {
        Guid productId = Guid.NewGuid();
        var request = new AddStockRequest(
            0,
            "INV-001");

        validator
            .Setup(x => x.Normalize(request))
            .Throws(
                new InventoryServiceException(
                    400,
                    "Quantity must be greater than zero."));

        await Assert.ThrowsAsync<InventoryServiceException>(
            () => CreateService().AddAsync(
                productId,
                request));

        stockAdder.Verify(
            x => x.AddAsync(
                It.IsAny<Guid>(),
                It.IsAny<InventoryStockRequest>()),
            Times.Never);
    }
}
