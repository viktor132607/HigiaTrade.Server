using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HygiaTrade.Tests.Unit.Seed;

public sealed class DemoReviewSeederTests
{
    private static ApplicationDbContext NewDb() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"DemoReviewSeeder-{Guid.NewGuid()}")
            .Options);

    private static async Task AddProducts(ApplicationDbContext db, int count)
    {
        for (int i = 0; i < count; i++)
        {
            db.Products.Add(new Product
            {
                Title = $"Product {i}",
                Description = "Description",
                MainImageUrl = "https://example.org/p.png",
                CategoryId = Guid.NewGuid(),
                IsActive = true
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SeedsExactlyQuarterWithHalfMultiReviewAndVaryingRatings()
    {
        await using var db = NewDb();
        await AddProducts(db, 96);
        DemoReviewSeeder.Result result = await DemoReviewSeeder.SeedAsync(db);

        Assert.Equal(12, result.UsersCreated);
        Assert.Equal(24, result.ProductsReviewed);
        Assert.Equal(42, result.ReviewsCreated);
        Assert.Equal(12, result.ProductsWithMultipleReviews);
        Assert.False(result.AlreadySeeded);

        List<Review> reviews = await db.Reviews.ToListAsync();
        Assert.Equal(24, reviews.Select(review => review.ProductId).Distinct().Count());
        Assert.Equal(12, reviews.GroupBy(review => review.ProductId).Count(g => g.Count() > 1));
        Assert.Equal(12, reviews.GroupBy(review => review.ProductId).Count(g => g.Count() == 1));
        Assert.Equal(42, reviews.GroupBy(review => new { review.ProductId, review.UserId }).Count());
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5 }, reviews.Select(review => review.Rating).Distinct().OrderBy(x => x).ToArray());
        Assert.All(reviews, review => Assert.StartsWith("[ТЕСТОВО РЕВЮ", review.Content));
        Assert.All(await db.Users.ToListAsync(), user =>
        {
            Assert.Contains("[ДЕМО]", user.Names);
            Assert.EndsWith(".invalid", user.Email);
            Assert.NotNull(user.PasswordHash);
        });
        Assert.All(await db.Products.Where(product => product.Rating > 0).ToListAsync(), product =>
        {
            double actualAverage = reviews.Where(review => review.ProductId == product.Id)
                .Average(review => (double)review.Rating);
            Assert.Equal(Math.Round(actualAverage, 2, MidpointRounding.AwayFromZero), product.Rating);
        });
    }

    [Fact]
    public async Task SecondRunDoesNotDuplicateOrRestoreDeletedReviews()
    {
        await using var db = NewDb();
        await AddProducts(db, 96);
        await DemoReviewSeeder.SeedAsync(db);
        Review review = await db.Reviews.FirstAsync();
        review.IsDeleted = true;
        await db.SaveChangesAsync();

        DemoReviewSeeder.Result rerun = await DemoReviewSeeder.SeedAsync(db);
        Assert.True(rerun.AlreadySeeded);
        Assert.Equal(0, rerun.ReviewsCreated);
        Assert.Equal(42, await db.Reviews.CountAsync());
        Assert.Equal(12, await db.Users.CountAsync());
        Assert.True((await db.Reviews.FindAsync(review.Id))!.IsDeleted);
    }

    [Fact]
    public async Task DoesNotTouchExistingCustomerReviewsOrInactiveProducts()
    {
        await using var db = NewDb();
        await AddProducts(db, 8);
        Product reviewed = await db.Products.OrderBy(product => product.Id).FirstAsync();
        User realUser = new()
        {
            Email = "customer@example.org",
            Names = "Real customer",
            Phone = "1234567890",
            PasswordHash = "unused"
        };
        db.Users.Add(realUser);
        db.Reviews.Add(new Review
        {
            ProductId = reviewed.Id,
            UserId = realUser.Id,
            Rating = 5,
            Content = "Original customer review"
        });
        Product inactive = await db.Products.OrderBy(product => product.Id).Skip(1).FirstAsync();
        inactive.IsActive = false;
        await db.SaveChangesAsync();

        DemoReviewSeeder.Result result = await DemoReviewSeeder.SeedAsync(db);
        Assert.Equal(2, result.ProductsReviewed);
        Assert.Equal(3, await db.Reviews.CountAsync(review => review.ProductId != reviewed.Id && !review.IsDeleted));
        Assert.Equal(1, await db.Reviews.CountAsync(review => review.ProductId == reviewed.Id));
        Assert.Equal(0, await db.Reviews.CountAsync(review => review.ProductId == inactive.Id));
        Assert.Equal("Original customer review",
            (await db.Reviews.SingleAsync(review => review.ProductId == reviewed.Id)).Content);
        Assert.Equal("Real customer", (await db.Users.SingleAsync(user => user.Id == realUser.Id)).Names);
    }

    [Fact]
    public async Task EmptyCatalogueDoesNotCreateUsers()
    {
        await using var db = NewDb();
        DemoReviewSeeder.Result result = await DemoReviewSeeder.SeedAsync(db);
        Assert.Equal(0, result.ReviewsCreated);
        Assert.Equal(0, await db.Users.CountAsync());
    }
}
