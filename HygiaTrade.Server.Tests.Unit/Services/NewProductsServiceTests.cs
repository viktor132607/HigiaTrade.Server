using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Domain.Interfaces;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class NewProductsServiceTests
{
    private readonly Mock<INewProductStatusRepository> repository = new();
    private readonly Mock<INewProductsPolicy> policy = new();
    private readonly Mock<INewProductsClock> clock = new();
    private readonly Mock<IProductService> productService = new();

    private NewProductsService CreateService() =>
        new(
            repository.Object,
            policy.Object,
            clock.Object,
            productService.Object);

    [Fact]
    public async Task GetAsync_NormalizesPageHydratesProductsAndKeepsRepositoryTotal()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        Guid third = Guid.NewGuid();
        Guid fourth = Guid.NewGuid();
        Guid fifth = Guid.NewGuid();

        policy
            .Setup(x => x.NormalizePage(0, 999))
            .Returns(new NewProductsPage(2, 2));

        repository
            .Setup(x => x.GetActiveProductIdsAsync())
            .ReturnsAsync(
                [first, second, third, fourth, fifth]);

        productService
            .Setup(x => x.GetByIdAsync(third))
            .ReturnsAsync(Product(third, true));

        productService
            .Setup(x => x.GetByIdAsync(fourth))
            .ReturnsAsync(Product(fourth, false));

        NewProductsPageResponse result =
            await CreateService().GetAsync(0, 999);

        Assert.Equal(5, result.TotalCount);
        Assert.Single(result.Items);

        ProductResponse item =
            Assert.IsType<ProductResponse>(
                result.Items[0]);

        Assert.Equal(third, item.Id);

        productService.Verify(
            x => x.GetByIdAsync(first),
            Times.Never);

        productService.Verify(
            x => x.GetByIdAsync(second),
            Times.Never);

        productService.Verify(
            x => x.GetByIdAsync(fifth),
            Times.Never);
    }

    [Fact]
    public async Task GetAsync_SkipsMissingProducts()
    {
        Guid id = Guid.NewGuid();

        policy
            .Setup(x => x.NormalizePage(1, 10))
            .Returns(new NewProductsPage(1, 10));

        repository
            .Setup(x => x.GetActiveProductIdsAsync())
            .ReturnsAsync([id]);

        productService
            .Setup(x => x.GetByIdAsync(id))
            .ReturnsAsync((ProductResponse?)null);

        NewProductsPageResponse result =
            await CreateService().GetAsync(1, 10);

        Assert.Equal(1, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetStatusAsync_ReadsRepositoryAndDelegatesStatusMapping()
    {
        Guid id = Guid.NewGuid();
        DateTime now =
            new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);

        var record =
            new NewProductStatusRecord(
                14,
                now.AddDays(14));

        var expected =
            new NewProductStatusDto(
                true,
                14,
                record.ActiveUntilUtc,
                true);

        repository
            .Setup(x => x.ReadAsync(id))
            .ReturnsAsync(record);

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(now);

        policy
            .Setup(x => x.CreateStatus(record, now))
            .Returns(expected);

        NewProductStatusDto result =
            await CreateService().GetStatusAsync(id);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task UpdateStatusAsync_Throws404_WhenProductDoesNotExist()
    {
        Guid id = Guid.NewGuid();
        var request =
            new UpdateNewProductStatusRequest(
                true,
                500);

        repository
            .Setup(x => x.ProductExistsAsync(id))
            .ReturnsAsync(false);

        NewProductsServiceException ex =
            await Assert.ThrowsAsync<NewProductsServiceException>(
                () => CreateService()
                    .UpdateStatusAsync(id, request));

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("Product not found.", ex.Message);

        policy.Verify(
            x => x.ValidateDisplayDays(
                It.IsAny<int>()),
            Times.Never);

        repository.Verify(
            x => x.UpsertAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_DisablesStatusWithoutValidatingDisplayDays()
    {
        Guid id = Guid.NewGuid();
        var request =
            new UpdateNewProductStatusRequest(
                false,
                999);

        var expected =
            new NewProductStatusDto(
                false,
                14,
                null,
                false);

        repository
            .Setup(x => x.ProductExistsAsync(id))
            .ReturnsAsync(true);

        policy
            .Setup(x => x.CreateInactiveStatus())
            .Returns(expected);

        NewProductStatusDto result =
            await CreateService().UpdateStatusAsync(
                id,
                request);

        Assert.Same(expected, result);

        repository.Verify(
            x => x.DeleteAsync(id),
            Times.Once);

        policy.Verify(
            x => x.ValidateDisplayDays(
                It.IsAny<int>()),
            Times.Never);

        repository.Verify(
            x => x.UpsertAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_DoesNotUpsert_WhenPolicyRejectsDisplayDays()
    {
        Guid id = Guid.NewGuid();
        var request =
            new UpdateNewProductStatusRequest(
                true,
                0);

        repository
            .Setup(x => x.ProductExistsAsync(id))
            .ReturnsAsync(true);

        policy
            .Setup(x => x.ValidateDisplayDays(0))
            .Throws(
                new NewProductsServiceException(
                    400,
                    "DisplayDays must be between 1 and 365."));

        await Assert.ThrowsAsync<NewProductsServiceException>(
            () => CreateService()
                .UpdateStatusAsync(id, request));

        repository.Verify(
            x => x.UpsertAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpsertsUsingClockThenReturnsStoredStatus()
    {
        Guid id = Guid.NewGuid();
        DateTime now =
            new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);

        var request =
            new UpdateNewProductStatusRequest(
                true,
                30);

        var stored =
            new NewProductStatusRecord(
                30,
                now.AddDays(30));

        var expected =
            new NewProductStatusDto(
                true,
                30,
                stored.ActiveUntilUtc,
                true);

        repository
            .Setup(x => x.ProductExistsAsync(id))
            .ReturnsAsync(true);

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(now);

        repository
            .Setup(x => x.ReadAsync(id))
            .ReturnsAsync(stored);

        policy
            .Setup(x => x.CreateStatus(stored, now))
            .Returns(expected);

        NewProductStatusDto result =
            await CreateService().UpdateStatusAsync(
                id,
                request);

        Assert.Same(expected, result);

        policy.Verify(
            x => x.ValidateDisplayDays(30),
            Times.Once);

        repository.Verify(
            x => x.UpsertAsync(
                id,
                30,
                now.AddDays(30)),
            Times.Once);

        repository.Verify(
            x => x.ReadAsync(id),
            Times.Once);
    }

    private static ProductResponse Product(
        Guid id,
        bool isActive) =>
        new()
        {
            Id = id,
            Title = "Product",
            Description = "Description",
            MainImageUrl = "image",
            IsActive = isActive,
            CategoryName = "Category"
        };
}
