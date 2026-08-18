using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Core.Pages;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.PaginationAndFiltering;
using HygiaTrade.Data.Repositories;

namespace HygiaTrade.Tests.Unit.Repositories;

public class ProductRepositoryTests
{
    private readonly ApplicationDbContext _context;
    private readonly ProductRepository _repository;

    public ProductRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new(options);
        _repository = new(_context);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnPaginatedResults_WhenFilterIsApplied()
    {
        Filter<Product> filter = new()
        {
            Predicate = p => p.Quantity > 0,
            PageNumber = 1,
            PageSize = 2,
            SortBy = "Title",
            SortDescending = false
        };
        
        Category category1 = new() { Name = "Category 1", ImageUri = "http://example.com/category1.jpg" };
        Category category2 = new() { Name = "Category 2", ImageUri = "http://example.com/category2.jpg" };

        Product product1 = new()
        {
            Title = "Product 1",
            Quantity = 10,
            Description = "Description for product 1",
            MainImageUrl = "http://example.com/image1.jpg",
            Category = category1
        };
        Product product2 = new()
        {
            Title = "Product 2",
            Quantity = 20,
            Description = "Description for product 2",
            MainImageUrl = "http://example.com/image2.jpg",
            Category = category2
        };
        
        _context.Categories.Add(category1);
        _context.Categories.Add(category2);
        _context.Products.Add(product1);
        _context.Products.Add(product2);
        await _context.SaveChangesAsync();

        Paginated<Product> result = await _repository.SearchAsync(filter);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count());
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        Guid productId = Guid.NewGuid();
        Category category = new() { Name = "Test Category", ImageUri = "http://example.com/category.jpg" };
        
        Product product = new()
        {
            Id = productId,
            Title = "Test Product",
            Quantity = 10,
            Description = "Description for test product",
            MainImageUrl = "http://example.com/image.jpg",
            Category = category
        };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        Product? result = await _repository.GetByIdAsync(productId);

        Assert.NotNull(result);
        Assert.Equal(productId, result?.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenProductIsDeleted()
    {
        Guid productId = Guid.NewGuid();
        Category category = new() { Name = "Test Category", ImageUri = "http://example.com/category.jpg" };
        
        Product product = new()
        {
            Id = productId,
            Title = "Test Product",
            Quantity = 10,
            IsDeleted = true,
            Description = "Description for test product",
            MainImageUrl = "http://example.com/image.jpg",
            Category = category
        };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        Product? result = await _repository.GetByIdAsync(productId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBestSellersAsync_ShouldReturnBestSellers_WhenCalled()
    {
        Category category1 = new() { Name = "Category 1", ImageUri = "http://example.com/category1.jpg" };
        Category category2 = new() { Name = "Category 2", ImageUri = "http://example.com/category2.jpg" };

        Product product1 = new()
        { 
            Title = "Product 1", 
            Quantity = 10, 
            Description = "Description for product 1", 
            MainImageUrl = "http://example.com/image1.jpg", 
            Category = category1 
        };
        Product product2 = new()
        { 
            Title = "Product 2", 
            Quantity = 20, 
            Description = "Description for product 2", 
            MainImageUrl = "http://example.com/image2.jpg", 
            Category = category2 
        };
        
        _context.Categories.Add(category1);
        _context.Categories.Add(category2);
        _context.Products.Add(product1);
        _context.Products.Add(product2);
        await _context.SaveChangesAsync();

        IEnumerable<Product> result = await _repository.GetBestSellersAsync(2);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task UpdateRatingAsync_ShouldUpdateProductRating_WhenCalled()
    {
        Guid productId = Guid.NewGuid();
        Category category = new() { Name = "Test Category", ImageUri = "http://example.com/category.jpg" };
        
        Product product = new()
        {
            Id = productId,
            Title = "Test Product",
            Rating = 3.0,
            Description = "Description for test product",
            MainImageUrl = "http://example.com/image.jpg",
            Category = category
        };
        _context.Categories.Add(category);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await _repository.UpdateRatingAsync(productId, 4.5);

        Product updatedProduct = await _repository.GetByIdAsync(productId);
        Assert.Equal(4.5, updatedProduct?.Rating);
    }
}
