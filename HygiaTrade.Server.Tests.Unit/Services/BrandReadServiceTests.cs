using HygiaTrade.Common.Responses.Brand;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class BrandReadServiceTests
{
    [Fact]
    public async Task GetAsync_MapsEveryRepositoryItem()
    {
        var repository =
            new Mock<IBrandRepository>();

        var mapper =
            new Mock<IBrandMapper>();

        BrandListItem first =
            new(
                Guid.NewGuid(),
                "A",
                null,
                null,
                1);

        BrandListItem second =
            new(
                Guid.NewGuid(),
                "B",
                "image",
                "desc",
                2);

        IReadOnlyList<BrandListItem> source =
            [first, second];

        repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(source);

        BrandResponse firstResponse =
            new(
                first.Id,
                first.Name,
                null,
                null,
                1);

        BrandResponse secondResponse =
            new(
                second.Id,
                second.Name,
                second.ThumbnailImageUrl,
                second.Description,
                2);

        mapper
            .Setup(x => x.ToResponse(first))
            .Returns(firstResponse);

        mapper
            .Setup(x => x.ToResponse(second))
            .Returns(secondResponse);

        var service =
            new BrandReadService(
                repository.Object,
                mapper.Object);

        IReadOnlyList<BrandResponse> result =
            await service.GetAsync();

        Assert.Equal(
            [firstResponse, secondResponse],
            result);
    }
}
