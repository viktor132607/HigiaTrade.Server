using System.Security.Claims;
using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Core.Pages;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class ProductsControllerTests
{
    private readonly Mock<IProductService> productService = new();
    private readonly Mock<IProductImagePresentationService> imageService = new();

    private ProductsController CreateController(bool isAdmin = false)
    {
        ProductsController controller =
            new(productService.Object, imageService.Object);

        List<Claim> claims = isAdmin
            ? [new Claim(ClaimTypes.Role, Roles.Admin)]
            : [];

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, "Test"))
            }
        };

        return controller;
    }

    [Fact]
    public async Task GetAllAsync_CreatesRequest_AndExcludesInactive_ForNonAdmin()
    {
        SearchProductsRequest? captured = null;
        Paginated<ProductsResponse> expected = new()
        {
            Items = [CreateProductsResponse()],
            TotalCount = 1
        };

        productService
            .Setup(service => service.SearchProductsAsync(
                It.IsAny<SearchProductsRequest>()))
            .Callback<SearchProductsRequest>(request => captured = request)
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().GetAllAsync(null);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        Assert.NotNull(captured);
        Assert.False(captured.IncludeInactive);

        imageService.Verify(
            service => service.ResolveAsync(
                It.IsAny<IEnumerable<ProductsResponse>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_UsesProvidedRequest_AndIncludesInactive_ForAdmin()
    {
        SearchProductsRequest request = new();

        productService
            .Setup(service => service.SearchProductsAsync(request))
            .ReturnsAsync(new Paginated<ProductsResponse>
            {
                Items = [CreateProductsResponse()],
                TotalCount = 1
            });

        IActionResult result =
            await CreateController(true).GetAllAsync(request);

        Assert.IsType<OkObjectResult>(result);
        Assert.True(request.IncludeInactive);
    }

    [Fact]
    public async Task GetBestSellersAsync_ReturnsOk_WhenProductsExist()
    {
        ProductResponse product = CreateProductResponse();

        productService
            .Setup(service => service.GetBestSellersAsync(3))
            .ReturnsAsync([product]);

        IActionResult result =
            await CreateController().GetBestSellersAsync(3);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        List<ProductResponse> value =
            Assert.IsType<List<ProductResponse>>(ok.Value);
        Assert.Single(value);

        imageService.Verify(
            service => service.ResolveAsync(
                It.IsAny<IEnumerable<ProductResponse>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetBestSellersAsync_ThrowsNotFound_WhenServiceReturnsNull()
    {
        productService
            .Setup(service => service.GetBestSellersAsync(3))
            .ReturnsAsync((IEnumerable<ProductResponse>?)null);

        await Assert.ThrowsAsync<AppException>(
            () => CreateController().GetBestSellersAsync(3));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsOk_AndResolvesImages_WhenFound()
    {
        Guid id = Guid.NewGuid();
        ProductResponse product = CreateProductResponse(id);

        productService
            .Setup(service => service.GetByIdAsync(id))
            .ReturnsAsync(product);

        IActionResult result =
            await CreateController().GetByIdAsync(id);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(product, ok.Value);

        imageService.Verify(
            service => service.ResolveAsync(
                It.Is<IEnumerable<ProductResponse>>(
                    items => items.Single() == product)),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsNotFound_AndDoesNotResolveImages_WhenMissing()
    {
        Guid id = Guid.NewGuid();

        productService
            .Setup(service => service.GetByIdAsync(id))
            .ReturnsAsync((ProductResponse?)null);

        await Assert.ThrowsAsync<AppException>(
            () => CreateController().GetByIdAsync(id));

        imageService.Verify(
            service => service.ResolveAsync(
                It.IsAny<IEnumerable<ProductResponse>>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPriceQuoteAsync_ReturnsOk()
    {
        Guid id = Guid.NewGuid();
        ProductPriceQuoteResponse expected = new()
        {
            ProductId = id,
            Quantity = 2
        };

        productService
            .Setup(service => service.GetPriceQuoteAsync(id, 2))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().GetPriceQuoteAsync(id, 2);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task CreateAsync_PreparesAndResolvesImages()
    {
        CreateProductRequest request = CreateProductRequest();
        ProductResponse expected = CreateProductResponse();

        productService
            .Setup(service => service.CreateAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().CreateAsync(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);

        imageService.Verify(
            service => service.PrepareForPersistenceAsync(request),
            Times.Once);
        imageService.Verify(
            service => service.ResolveAsync(
                It.IsAny<IEnumerable<ProductResponse>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_DoesNotResolveImages_WhenServiceReturnsNull()
    {
        CreateProductRequest request = CreateProductRequest();

        productService
            .Setup(service => service.CreateAsync(request))
            .ReturnsAsync((ProductResponse?)null);

        await Assert.ThrowsAsync<AppException>(
            () => CreateController().CreateAsync(request));

        imageService.Verify(
            service => service.PrepareForPersistenceAsync(request),
            Times.Once);
        imageService.Verify(
            service => service.ResolveAsync(
                It.IsAny<IEnumerable<ProductResponse>>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_PreparesAndResolvesImages()
    {
        UpdateProductRequest request = CreateUpdateProductRequest();
        ProductResponse expected = CreateProductResponse(request.Id);

        productService
            .Setup(service => service.UpdateAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().UpdateAsync(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);

        imageService.Verify(
            service => service.PrepareForPersistenceAsync(request),
            Times.Once);
        imageService.Verify(
            service => service.ResolveAsync(
                It.IsAny<IEnumerable<ProductResponse>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_DoesNotResolveImages_WhenServiceReturnsNull()
    {
        UpdateProductRequest request = CreateUpdateProductRequest();

        productService
            .Setup(service => service.UpdateAsync(request))
            .ReturnsAsync((ProductResponse?)null);

        await Assert.ThrowsAsync<AppException>(
            () => CreateController().UpdateAsync(request));

        imageService.Verify(
            service => service.ResolveAsync(
                It.IsAny<IEnumerable<ProductResponse>>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsOk_WhenDeleted()
    {
        Guid id = Guid.NewGuid();

        productService
            .Setup(service => service.DeleteAsync(id))
            .ReturnsAsync(true);

        IActionResult result =
            await CreateController().DeleteAsync(id);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsNotFound_WhenServiceReturnsFalse()
    {
        Guid id = Guid.NewGuid();

        productService
            .Setup(service => service.DeleteAsync(id))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<AppException>(
            () => CreateController().DeleteAsync(id));
    }

    private static ProductResponse CreateProductResponse(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Title = "Product",
        Description = "Description",
        MainImageUrl = "/product.png",
        CategoryName = "Category"
    };

    private static ProductsResponse CreateProductsResponse() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Product",
        Description = "Description",
        MainImageUrl = "/product.png",
        CategoryName = "Category"
    };

    private static CreateProductRequest CreateProductRequest() => new()
    {
        Title = "Product",
        Description = "Description",
        MainImageUrl = "/product.png",
        CategoryId = Guid.NewGuid()
    };

    private static UpdateProductRequest CreateUpdateProductRequest() => new()
    {
        Id = Guid.NewGuid(),
        Title = "Product",
        Description = "Description",
        MainImageUrl = "/product.png",
        CategoryId = Guid.NewGuid()
    };
}
