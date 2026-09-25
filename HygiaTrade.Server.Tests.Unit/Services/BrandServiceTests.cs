using HygiaTrade.Common.Requests.Brand;
using HygiaTrade.Common.Responses.Brand;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class BrandServiceTests
{
    private readonly Mock<IBrandReadService> readService = new();
    private readonly Mock<IBrandMutationService> mutationService = new();

    private BrandService CreateService() =>
        new(readService.Object, mutationService.Object);

    [Fact]
    public async Task GetAsync_DelegatesToReadService()
    {
        IReadOnlyList<BrandResponse> expected =
        [
            new(
                Guid.NewGuid(),
                "Brand",
                null,
                null,
                2)
        ];

        readService
            .Setup(x => x.GetAsync())
            .ReturnsAsync(expected);

        IReadOnlyList<BrandResponse> result =
            await CreateService().GetAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task CreateAsync_DelegatesToMutationService()
    {
        var request =
            new BrandRequest(
                "Brand",
                null,
                null);

        var expected =
            new BrandResponse(
                Guid.NewGuid(),
                "Brand",
                null,
                null,
                0);

        mutationService
            .Setup(x => x.CreateAsync(request))
            .ReturnsAsync(expected);

        BrandResponse result =
            await CreateService().CreateAsync(request);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task UpdateAsync_DelegatesToMutationService()
    {
        var request =
            new UpdateBrandRequest(
                Guid.NewGuid(),
                "Brand",
                null,
                null);

        var expected =
            new BrandResponse(
                request.Id,
                "Brand",
                null,
                null,
                3);

        mutationService
            .Setup(x => x.UpdateAsync(request))
            .ReturnsAsync(expected);

        BrandResponse result =
            await CreateService().UpdateAsync(request);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToMutationService()
    {
        Guid id = Guid.NewGuid();

        await CreateService().DeleteAsync(id);

        mutationService.Verify(
            x => x.DeleteAsync(id),
            Times.Once);
    }
}
