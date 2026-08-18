using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Common.Requests.Category;
using HygiaTrade.Common.Responses.Category;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Data.Repositories;
using HygiaTrade.Domain.Interfaces;
using HygiaTrade.Domain.Services;
using Xunit;

namespace HygiaTrade.Tests.Unit.Services;

public class CategoryServiceTests
{
    private readonly ICategoryService _categoryService;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ApplicationDbContext _dbContext;

    public CategoryServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new(options);
        _categoryRepository = new CategoryRepository(_dbContext);
        _categoryService = new CategoryService(_categoryRepository, new ImageRepository(_dbContext));
    }

    private async Task ClearDatabaseAsync()
    {
        _dbContext.Categories.RemoveRange(_dbContext.Categories);
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnCategoryResponses_WhenCategoriesExist()
    {
        await ClearDatabaseAsync();

        Category category1 = new()
        {
            Name = "Category 1",
            ImageUri = "http://example.com/category1.jpg"
        };
        Category category2 = new()
        {
            Name = "Category 2",
            ImageUri = "http://example.com/category2.jpg"
        };
        await _categoryRepository.AddAsync(category1);
        await _categoryRepository.AddAsync(category2);

        IEnumerable<CategoryResponse>? result = await _categoryService.GetAsync();

        Assert.NotNull(result);
        Assert.Contains(result, r => r.Name == category1.Name);
        Assert.Contains(result, r => r.Name == category2.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnCategoryResponse_WhenCategoryExists()
    {
        await ClearDatabaseAsync();

        Category category = new()
        {
            Name = "Test Category",
            ImageUri = "http://example.com/category.jpg"
        };
        Category? addedCategory = await _categoryRepository.AddAsync(category);

        CategoryResponse? result = await _categoryService.GetByIdAsync(addedCategory.Id);

        Assert.NotNull(result);
        Assert.Equal(addedCategory.Name, result.Name);
        Assert.Equal(addedCategory.ImageUri, result.ImageURI);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateCategory_WhenValidRequest()
    {
        await ClearDatabaseAsync();

        CreateCategoryRequest request = new()
        {
            Name = "New Category",
            ImageURI = "http://example.com/new_category.jpg"
        };

        CategoryResponse? result = await _categoryService.CreateAsync(request);

        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.ImageURI, result.ImageURI);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateCategory_WhenCategoryExists()
    {
        await ClearDatabaseAsync();

        Category category = new()
        {
            Name = "Old Category",
            ImageUri = "http://example.com/old_category.jpg"
        };
        Category? addedCategory = await _categoryRepository.AddAsync(category);
        UpdateCategoryRequest request = new()
        {
            Id = addedCategory.Id,
            Name = "Updated Category",
            ImageURI = "http://example.com/updated_category.jpg"
        };

        CategoryResponse? result = await _categoryService.UpdateAsync(request);

        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.ImageURI, result.ImageURI);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteCategory_WhenCategoryExists()
    {
        await ClearDatabaseAsync();

        Category category = new()
        {
            Name = "Category To Delete",
            ImageUri = "http://example.com/delete_category.jpg"
        };
        Category? addedCategory = await _categoryRepository.AddAsync(category);

        bool result = await _categoryService.DeleteAsync(addedCategory.Id);

        Assert.True(result);
        Category? deletedCategory = await _categoryRepository.GetByIdAsync(addedCategory.Id);
        Assert.Null(deletedCategory);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowException_WhenCategoryNotFound()
    {
        await ClearDatabaseAsync();

        Guid invalidId = Guid.NewGuid();

        await Assert.ThrowsAsync<AppException>(() => _categoryService.GetByIdAsync(invalidId));
    }


[Fact]
    public async Task UpdateAsync_ShouldThrowException_WhenCategoryNotFound()
    {
        await ClearDatabaseAsync();

        UpdateCategoryRequest request = new() { Id = Guid.NewGuid(), Name = "Updated Category", ImageURI = "http://example.com/updated_category.jpg" };

        await Assert.ThrowsAsync<AppException>(() => _categoryService.UpdateAsync(request));
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowException_WhenCategoryNotFound()
    {
        await ClearDatabaseAsync();

        Guid invalidId = Guid.NewGuid();

        await Assert.ThrowsAsync<AppException>(() => _categoryService.DeleteAsync(invalidId));
    }
}
