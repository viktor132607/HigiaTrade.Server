using HygiaTrade.Common.Requests.Brand;
using HygiaTrade.Common.Responses.Brand;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class BrandMutationServiceTests
{
    [Fact]
    public async Task CreateAsync_NormalizesChecksDuplicatePersistsAndMaps()
    {
        var repository = new Mock<IBrandRepository>();
        var policy = new Mock<IBrandPolicy>();
        var mapper = new Mock<IBrandMapper>();
        var clock = new Mock<IBrandClock>();

        var request =
            new BrandRequest(
                "  Acme  ",
                " image ",
                " desc ");

        policy
            .Setup(x => x.NormalizeName(request.Name))
            .Returns("Acme");

        policy
            .Setup(x => x.NormalizeOptional(
                request.ThumbnailImageUrl))
            .Returns("image");

        policy
            .Setup(x => x.NormalizeOptional(
                request.Description))
            .Returns("desc");

        repository
            .Setup(x => x.ExistsByNameAsync(
                "Acme",
                null))
            .ReturnsAsync(false);

        Brand? captured = null;

        repository
            .Setup(x => x.Add(
                It.IsAny<Brand>()))
            .Callback<Brand>(
                brand => captured = brand);

        BrandResponse expected =
            new(
                Guid.NewGuid(),
                "Acme",
                "image",
                "desc",
                0);

        mapper
            .Setup(x => x.ToResponse(
                It.IsAny<Brand>(),
                0))
            .Returns(expected);

        var service =
            new BrandMutationService(
                repository.Object,
                policy.Object,
                mapper.Object,
                clock.Object);

        BrandResponse result =
            await service.CreateAsync(request);

        Assert.Same(expected, result);
        Assert.NotNull(captured);
        Assert.Equal("Acme", captured.Name);
        Assert.Equal("image", captured.ThumbnailImageUrl);
        Assert.Equal("desc", captured.Description);

        repository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateBeforeAdding()
    {
        var repository =
            new Mock<IBrandRepository>();

        repository
            .Setup(x => x.ExistsByNameAsync(
                "Acme",
                null))
            .ReturnsAsync(true);

        var service =
            new BrandMutationService(
                repository.Object,
                new BrandPolicy(),
                Mock.Of<IBrandMapper>(),
                Mock.Of<IBrandClock>());

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => service.CreateAsync(
                    new BrandRequest(
                        "Acme",
                        null,
                        null)));

        Assert.Equal(409, ex.StatusCode);

        repository.Verify(
            x => x.Add(It.IsAny<Brand>()),
            Times.Never);

        repository.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RenamesProductsUpdatesBrandAndMapsCount()
    {
        Brand brand =
            new()
            {
                Name = "Old",
                ThumbnailImageUrl = "old-image",
                Description = "old-desc"
            };

        Guid id = brand.Id;

        var repository =
            new Mock<IBrandRepository>();

        var policy =
            new Mock<IBrandPolicy>();

        var mapper =
            new Mock<IBrandMapper>();

        var clock =
            new Mock<IBrandClock>();

        DateTime now =
            new(
                2026,
                9,
                25,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var request =
            new UpdateBrandRequest(
                id,
                " New ",
                " image ",
                " desc ");

        repository
            .Setup(x => x.GetTrackedAsync(id))
            .ReturnsAsync(brand);

        policy
            .Setup(x => x.NormalizeName(request.Name))
            .Returns("New");

        repository
            .Setup(x => x.ExistsByNameAsync(
                "New",
                id))
            .ReturnsAsync(false);

        policy
            .Setup(x => x.NormalizeOptional(
                request.ThumbnailImageUrl))
            .Returns("image");

        policy
            .Setup(x => x.NormalizeOptional(
                request.Description))
            .Returns("desc");

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(now);

        repository
            .Setup(x => x.GetActiveProductCountAsync(
                "New"))
            .ReturnsAsync(4);

        BrandResponse expected =
            new(
                id,
                "New",
                "image",
                "desc",
                4);

        mapper
            .Setup(x => x.ToResponse(
                brand,
                4))
            .Returns(expected);

        var service =
            new BrandMutationService(
                repository.Object,
                policy.Object,
                mapper.Object,
                clock.Object);

        BrandResponse result =
            await service.UpdateAsync(request);

        Assert.Same(expected, result);
        Assert.Equal("New", brand.Name);
        Assert.Equal("image", brand.ThumbnailImageUrl);
        Assert.Equal("desc", brand.Description);
        Assert.Equal(now, brand.ModifiedOn);

        repository.Verify(
            x => x.RenameProductsAsync(
                "Old",
                "New",
                now),
            Times.Once);

        repository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotRenameProducts_WhenNameIsExactlyUnchanged()
    {
        Brand brand =
            new()
            {
                Name = "Acme"
            };

        var repository =
            new Mock<IBrandRepository>();

        repository
            .Setup(x => x.GetTrackedAsync(brand.Id))
            .ReturnsAsync(brand);

        repository
            .Setup(x => x.ExistsByNameAsync(
                "Acme",
                brand.Id))
            .ReturnsAsync(false);

        repository
            .Setup(x => x.GetActiveProductCountAsync(
                "Acme"))
            .ReturnsAsync(0);

        var policy =
            new Mock<IBrandPolicy>();

        policy
            .Setup(x => x.NormalizeName("Acme"))
            .Returns("Acme");

        policy
            .Setup(x => x.NormalizeOptional(
                It.IsAny<string?>()))
            .Returns((string?)null);

        var mapper =
            new Mock<IBrandMapper>();

        mapper
            .Setup(x => x.ToResponse(
                brand,
                0))
            .Returns(
                new BrandResponse(
                    brand.Id,
                    "Acme",
                    null,
                    null,
                    0));

        var clock =
            new Mock<IBrandClock>();

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(DateTime.UtcNow);

        var service =
            new BrandMutationService(
                repository.Object,
                policy.Object,
                mapper.Object,
                clock.Object);

        await service.UpdateAsync(
            new UpdateBrandRequest(
                brand.Id,
                "Acme",
                null,
                null));

        repository.Verify(
            x => x.RenameProductsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Throws404_WhenBrandDoesNotExist()
    {
        var service =
            new BrandMutationService(
                Mock.Of<IBrandRepository>(),
                new BrandPolicy(),
                Mock.Of<IBrandMapper>(),
                Mock.Of<IBrandClock>());

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => service.UpdateAsync(
                    new UpdateBrandRequest(
                        Guid.NewGuid(),
                        "Acme",
                        null,
                        null)));

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("Brand not found.", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_RejectsAssignedBrand()
    {
        Brand brand =
            new()
            {
                Name = "Acme"
            };

        var repository =
            new Mock<IBrandRepository>();

        repository
            .Setup(x => x.GetTrackedAsync(brand.Id))
            .ReturnsAsync(brand);

        repository
            .Setup(x => x.GetAssignedProductCountAsync(
                "Acme"))
            .ReturnsAsync(3);

        var service =
            new BrandMutationService(
                repository.Object,
                new BrandPolicy(),
                Mock.Of<IBrandMapper>(),
                Mock.Of<IBrandClock>());

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => service.DeleteAsync(
                    brand.Id));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("3 product(s)", ex.Message);
        Assert.False(brand.IsDeleted);

        repository.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesAndUsesClock()
    {
        Brand brand =
            new()
            {
                Name = "Acme"
            };

        DateTime now =
            new(
                2026,
                9,
                25,
                13,
                0,
                0,
                DateTimeKind.Utc);

        var repository =
            new Mock<IBrandRepository>();

        repository
            .Setup(x => x.GetTrackedAsync(brand.Id))
            .ReturnsAsync(brand);

        repository
            .Setup(x => x.GetAssignedProductCountAsync(
                "Acme"))
            .ReturnsAsync(0);

        var clock =
            new Mock<IBrandClock>();

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(now);

        var service =
            new BrandMutationService(
                repository.Object,
                new BrandPolicy(),
                Mock.Of<IBrandMapper>(),
                clock.Object);

        await service.DeleteAsync(brand.Id);

        Assert.True(brand.IsDeleted);
        Assert.Equal(now, brand.ModifiedOn);

        repository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }
}
