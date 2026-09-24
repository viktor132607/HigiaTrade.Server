using HygiaTrade.API.Controllers;
using HygiaTrade.Common.Requests.Brand;
using HygiaTrade.Common.Responses.Brand;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class BrandsControllerTests
{
    private readonly Mock<IBrandService> brandService = new();

    private BrandsController CreateController() =>
        new(brandService.Object);

    [Fact]
    public async Task GetAsync_ReturnsOk_WithBrands()
    {
        IReadOnlyList<BrandResponse> expected =
        [
            new BrandResponse(
                Guid.NewGuid(),
                "Brand A",
                "/brand-a.png",
                "Description A",
                3),
            new BrandResponse(
                Guid.NewGuid(),
                "Brand B",
                null,
                null,
                0)
        ];

        brandService
            .Setup(service => service.GetAsync())
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().GetAsync();

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);

        brandService.Verify(
            service => service.GetAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsOk_WithCreatedBrand()
    {
        BrandRequest request = new(
            "Brand A",
            "/brand-a.png",
            "Description");

        BrandResponse expected = new(
            Guid.NewGuid(),
            request.Name,
            request.ThumbnailImageUrl,
            request.Description,
            0);

        brandService
            .Setup(service => service.CreateAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().CreateAsync(request);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);

        brandService.Verify(
            service => service.CreateAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsOk_WithUpdatedBrand()
    {
        Guid id = Guid.NewGuid();

        UpdateBrandRequest request = new(
            id,
            "Updated Brand",
            "/updated.png",
            "Updated description");

        BrandResponse expected = new(
            id,
            request.Name,
            request.ThumbnailImageUrl,
            request.Description,
            4);

        brandService
            .Setup(service => service.UpdateAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().UpdateAsync(request);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);

        brandService.Verify(
            service => service.UpdateAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsOk_AndCallsService()
    {
        Guid id = Guid.NewGuid();

        brandService
            .Setup(service => service.DeleteAsync(id))
            .Returns(Task.CompletedTask);

        IActionResult result =
            await CreateController().DeleteAsync(id);

        Assert.IsType<OkResult>(result);

        brandService.Verify(
            service => service.DeleteAsync(id),
            Times.Once);
    }
}
