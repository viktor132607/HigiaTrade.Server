using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Xunit;
using HygiaTrade.Data.Repositories;

namespace HygiaTrade.Tests.Unit.Repositories;

public class ReviewRepositoryTests
{
    private readonly ReviewRepository _repository;
    private readonly ApplicationDbContext _context;

    public ReviewRepositoryTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        ApplicationDbContext context = new(options);
        _context = context;
        ReviewRepository repository = new(_context);
        _repository = repository;
    }

    [Fact]
    public async Task GetReviews_ShouldReturnReviews_WhenReviewsExistForProduct()
    {
        Guid productId = Guid.NewGuid();
        Review review1 = new();
        review1.ProductId = productId;
        review1.Content = "Great product!";
        review1.Rating = 5;
        Review review2 = new();
        review2.ProductId = productId;
        review2.Content = "Not bad";
        review2.Rating = 3;
        _context.Reviews.Add(review1);
        _context.Reviews.Add(review2);
        await _context.SaveChangesAsync();
        IEnumerable<Review> result = await _repository.GetReviews(productId);
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetReviews_ShouldReturnEmpty_WhenNoReviewsExistForProduct()
    {
        Guid productId = Guid.NewGuid();
        IEnumerable<Review> result = await _repository.GetReviews(productId);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetReviews_ShouldNotReturnDeletedReviews()
    {
        Guid productId = Guid.NewGuid();
        Review review1 = new();
        review1.ProductId = productId;
        review1.Content = "Great product!";
        review1.Rating = 5;
        review1.IsDeleted = true;
        Review review2 = new();
        review2.ProductId = productId;
        review2.Content = "Not bad";
        review2.Rating = 3;
        _context.Reviews.Add(review1);
        _context.Reviews.Add(review2);
        await _context.SaveChangesAsync();
        IEnumerable<Review> result = await _repository.GetReviews(productId);
        Assert.Single(result);
        Assert.Equal("Not bad", result.First().Content);
    }
}
