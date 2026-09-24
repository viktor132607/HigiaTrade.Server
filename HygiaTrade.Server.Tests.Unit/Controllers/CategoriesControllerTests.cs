using HygiaTrade.API.Controllers;
using HygiaTrade.Common.Requests.Category;
using HygiaTrade.Common.Responses.Category;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class CategoriesControllerTests
{
    private readonly Mock<ICategoryService> categoryService = new();

    private CategoriesController CreateController() =>
        new(categoryService.Object);

    [Fact]
    public async Task GetAllAsync_ReturnsOk_WithCategories()
    {
        IEnumerable<CategoryResponse> expected =
        [
            CreateCategoryResponse("Cleaning"),
            CreateCategoryResponse("Laundry")
        ];

        categoryService
            .Setup(service => service.GetAsync())
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().GetAllAsync();

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);

        categoryService.Verify(
            service => service.GetAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOk_WithEmptyCollection_WhenServiceReturnsNull()
    {
        categoryService
            .Setup(service => service.GetAsync())
            .ReturnsAsync((IEnumerable<CategoryResponse>?)null);

        IActionResult result =
            await CreateController().GetAllAsync();

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(ok.Value);
        Assert.Empty(Assert.IsAssignableFrom<System.Collections.IEnumerable>(ok.Value));

        categoryService.Verify(
            service => service.GetAsync(),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsOk_WhenCategoryExists()
    {
        Guid id = Guid.NewGuid();
        CategoryResponse expected =
            CreateCategoryResponse("Cleaning", id);

        categoryService
            .Setup(service => service.GetByIdAsync(id))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().GetByIdAsync(id);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetByIdAsync_ThrowsAppException_WhenCategoryIsMissing()
    {
        Guid id = Guid.NewGuid();

        categoryService
            .Setup(service => service.GetByIdAsync(id))
            .ReturnsAsync((CategoryResponse?)null);

        await Assert.ThrowsAsync<AppException>(
            () => CreateController().GetByIdAsync(id));
    }

    [Fact]
    public async Task CreateAsync_ReturnsOk_WhenModelStateIsValid()
    {
        CreateCategoryRequest request = new()
        {
            Name = "Cleaning",
            ImageURI = "/cleaning.png"
        };

        CategoryResponse expected =
            CreateCategoryResponse(request.Name);

        categoryService
            .Setup(service => service.CreateAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().CreateAsync(request);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);

        categoryService.Verify(
            service => service.CreateAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        CreateCategoryRequest request = new()
        {
            Name = string.Empty
        };

        CategoriesController controller =
            CreateController();

        controller.ModelState.AddModelError(
            nameof(request.Name),
            "Name is required.");

        IActionResult result =
            await controller.CreateAsync(request);

        Assert.IsType<BadRequestObjectResult>(result);

        categoryService.Verify(
            service => service.CreateAsync(
                It.IsAny<CreateCategoryRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsOk_WhenModelStateIsValid()
    {
        UpdateCategoryRequest request = new()
        {
            Id = Guid.NewGuid(),
            Name = "Updated",
            ImageURI = "/updated.png"
        };

        CategoryResponse expected =
            CreateCategoryResponse(
                request.Name,
                request.Id);

        categoryService
            .Setup(service => service.UpdateAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().UpdateAsync(request);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);

        categoryService.Verify(
            service => service.UpdateAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        UpdateCategoryRequest request = new()
        {
            Id = Guid.NewGuid(),
            Name = string.Empty
        };

        CategoriesController controller =
            CreateController();

        controller.ModelState.AddModelError(
            nameof(request.Name),
            "Name is required.");

        IActionResult result =
            await controller.UpdateAsync(request);

        Assert.IsType<BadRequestObjectResult>(result);

        categoryService.Verify(
            service => service.UpdateAsync(
                It.IsAny<UpdateCategoryRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsOk_WhenServiceDeletesCategory()
    {
        Guid id = Guid.NewGuid();

        categoryService
            .Setup(service => service.DeleteAsync(id))
            .ReturnsAsync(true);

        IActionResult result =
            await CreateController().DeleteAsync(id);

        Assert.IsType<OkResult>(result);

        categoryService.Verify(
            service => service.DeleteAsync(id),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsAppException_WhenServiceReturnsFalse()
    {
        Guid id = Guid.NewGuid();

        categoryService
            .Setup(service => service.DeleteAsync(id))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<AppException>(
            () => CreateController().DeleteAsync(id));
    }

    private static CategoryResponse CreateCategoryResponse(
        string name,
        Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name
        };
}
