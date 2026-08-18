using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Core.Pages;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.PaginationAndFiltering;
using HygiaTrade.Data.Repositories;
using Xunit;

namespace HygiaTrade.Tests.Unit.Repositories;

public class RepositoryTests
{
    private readonly ApplicationDbContext _context;
    private readonly Repository<Image> _repository;

    public RepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        _context = new(options);
        _repository = new(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntity()
    {
        await ClearDatabaseAsync();

        Image image = new()
        {
            Uri = "http://example.com/image.jpg",
            ProductId = Guid.NewGuid()
        };

        Image? result = await _repository.AddAsync(image);

        Assert.NotNull(result);
        Assert.Equal(image.Uri, result.Uri);
        Assert.False(result.IsDeleted);
    }

    [Fact]
    public async Task AddAsync_ShouldAddMultipleEntities()
    {
        await ClearDatabaseAsync();

        Image image1 = new() { Uri = "http://example.com/image1.jpg" };
        Image image2 = new() { Uri = "http://example.com/image2.jpg" };
        await _repository.AddAsync(image1);
        await _repository.AddAsync(image2);

        IEnumerable<Image> result = await _repository.GetAllAsync();

        Assert.Equal(2, result.Count());
        Assert.Contains(result, img => img.Id == image1.Id);
        Assert.Contains(result, img => img.Id == image2.Id);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnNonDeletedEntities()
    {
        await ClearDatabaseAsync();

        Image image1 = new() { Uri = "http://example.com/image1.jpg" };
        Image image2 = new()
        {
            Uri = "http://example.com/image2.jpg",
            IsDeleted = true
        };
        await _repository.AddAsync(image1);
        await _repository.AddAsync(image2);

        IEnumerable<Image> result = await _repository.GetAllAsync();

        Assert.Single(result);
        Assert.Contains(result, img => img.Id == image1.Id);
        Assert.DoesNotContain(result, img => img.Id == image2.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity_WhenNotDeleted()
    {
        await ClearDatabaseAsync();

        Image image = new() { Uri = "http://example.com/image.jpg" };
        Image? addedImage = await _repository.AddAsync(image);

        Image? result = await _repository.GetByIdAsync(addedImage.Id);

        Assert.NotNull(result);
        Assert.Equal(addedImage.Id, result?.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEntityIsDeleted()
    {
        await ClearDatabaseAsync();

        Image image = new()
        {
            Uri = "http://example.com/image.jpg",
            IsDeleted = true
        };
        Image? addedImage = await _repository.AddAsync(image);

        Image? result = await _repository.GetByIdAsync(addedImage.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldMarkEntityAsDeleted_WhenPropertyExists()
    {
        await ClearDatabaseAsync();

        Image image = new() { Uri = "http://example.com/image.jpg" };
        Image? addedImage = await _repository.AddAsync(image);

        bool result = await _repository.DeleteAsync(addedImage.Id);

        Assert.True(result);
        Image? deletedImage = await _repository.GetByIdAsync(addedImage.Id);
        Assert.Null(deletedImage);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntity_WhenPropertyDoesNotExist()
    {
        await ClearDatabaseAsync();

        Image image = new() { Uri = "http://example.com/image.jpg" };
        Image? addedImage = await _repository.AddAsync(image);

        bool result = await _repository.DeleteAsync(addedImage.Id);

        Assert.True(result);

        Image? deletedImage = await _repository.GetByIdAsync(addedImage.Id);
        Assert.Null(deletedImage);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenEntityDoesNotExist()
    {
        await ClearDatabaseAsync();

        bool result = await _repository.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntity()
    {
        await ClearDatabaseAsync();

        Image image = new() { Uri = "http://example.com/image.jpg" };
        Image? addedImage = await _repository.AddAsync(image);

        addedImage.Uri = "http://example.com/updated_image.jpg";
        Image? updatedImage = await _repository.UpdateAsync(addedImage);

        Assert.NotNull(updatedImage);
        Assert.Equal("http://example.com/updated_image.jpg", updatedImage?.Uri);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenEntityDoesNotExist()
    {
        await ClearDatabaseAsync();

        Image image = new() { Uri = "http://example.com/image.jpg" };

        Image? result = await _repository.UpdateAsync(image);

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnCorrectPaginatedResults()
    {
        await ClearDatabaseAsync();

        Image image1 = new() { Uri = "http://example.com/image1.jpg" };
        Image image2 = new() { Uri = "http://example.com/image2.jpg" };
        await _repository.AddAsync(image1);
        await _repository.AddAsync(image2);

        Filter<Image> filter = new()
        {
            Predicate = x => true,
            PageNumber = 1,
            PageSize = 1
        };
        Paginated<Image> result = await _repository.SearchAsync(filter);

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnSortedResults()
    {
        await ClearDatabaseAsync();

        Image image1 = new() { Uri = "http://example.com/image1.jpg" };
        Image image2 = new() { Uri = "http://example.com/image2.jpg" };
        await _repository.AddAsync(image1);
        await _repository.AddAsync(image2);

        Filter<Image> filter = new()
        {
            Predicate = x => true,
            SortBy = "Uri",
            SortDescending = true,
            PageNumber = 1,
            PageSize = 2
        };

        Paginated<Image> result = await _repository.SearchAsync(filter);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal("http://example.com/image2.jpg", result.Items.First().Uri);
        Assert.Equal("http://example.com/image1.jpg", result.Items.Last().Uri);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnFilteredResults()
    {
        await ClearDatabaseAsync();

        Image image1 = new() { Uri = "http://example.com/image1.jpg" };
        Image image2 = new() { Uri = "http://example.com/image2.jpg" };
        await _repository.AddAsync(image1);
        await _repository.AddAsync(image2);

        Filter<Image> filter = new()
        {
            Predicate = x => x.Uri.Contains("image1"),
            PageNumber = 1,
            PageSize = 1
        };

        Paginated<Image> result = await _repository.SearchAsync(filter);

        Assert.Single(result.Items);
        Assert.Contains(result.Items, img => img.Id == image1.Id);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnNull_WhenEntityIsNull()
    {
        await ClearDatabaseAsync();

        Image? result = await _repository.AddAsync(null);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEntityNotFound()
    {
        await ClearDatabaseAsync();

        Image? result = await _repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenEntityIsNull()
    {
        await ClearDatabaseAsync();

        Image? result = await _repository.UpdateAsync(null);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenEntityNotFound()
    {
        await ClearDatabaseAsync();

        bool result = await _repository.DeleteAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmpty_WhenPredicateIsInvalid()
    {
        await ClearDatabaseAsync();

        Filter<Image> filter = new()
        {
            Predicate = x => x.Uri.Contains("nonexistent"),
            PageNumber = 1,
            PageSize = 1
        };

        Paginated<Image> result = await _repository.SearchAsync(filter);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnAllEntities_WhenNoFilterIsApplied()
    {
        await ClearDatabaseAsync();

        Image image1 = new() { Uri = "http://example.com/image1.jpg" };
        Image image2 = new() { Uri = "http://example.com/image2.jpg" };
        await _repository.AddAsync(image1);
        await _repository.AddAsync(image2);

        Filter<Image> filter = new()
        {
            Predicate = x => true,
            PageNumber = 1,
            PageSize = 10
        };

        Paginated<Image> result = await _repository.SearchAsync(filter);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }
    
    private async Task ClearDatabaseAsync()
    {
        _context.Images.RemoveRange(_context.Images); 
        await _context.SaveChangesAsync();
    }
}
