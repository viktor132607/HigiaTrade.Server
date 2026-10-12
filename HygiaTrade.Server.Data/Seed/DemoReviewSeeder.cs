using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Seed;

/// <summary>
/// Opt-in demo-only review fixture. Never seed unlabelled testimonials.
/// After the first successful run, the first demo account acts as a marker,
/// preventing deleted test reviews from being recreated on later deploys.
/// Real customer accounts, orders and existing reviews are never changed.
/// </summary>
public static class DemoReviewSeeder
{
    private const string EmailDomain = "tests.higiatrade.invalid";
    private const string TestMarkerEmail = "demo-review-01@" + EmailDomain;
    private const int DemoUserCount = 12;

    private static readonly string[] ReviewSamples =
    [
        "Тест на визуализацията на кратък коментар.",
        "Проверка на рейтинг, дата и автор в изгледа на продукта.",
        "Тест на по-дълъг коментар, който проверява дали текстът се пренася правилно на следващ ред без да нарушава подравняването на останалите елементи.",
        "Демонстрация на различните стойности на звездния рейтинг.",
        "Проверка на подреждането при няколко ревюта на един продукт.",
        "Тест на едновременно показване на текст, потребител и оценка.",
        "Проверка на обновяването на средния рейтинг и броя коментари.",
        "Демонстрационен коментар за проверка на мобилния изглед."
    ];

    public sealed record Result(
        int UsersCreated,
        int ProductsReviewed,
        int ReviewsCreated,
        int ProductsWithMultipleReviews,
        bool AlreadySeeded = false);

    public static async Task<Result> SeedAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        // Relational databases use a transaction so the sentinel, reviews and
        // product averages are committed together.
        if (db.Database.IsRelational())
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            Result result = await SeedCoreAsync(db, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        return await SeedCoreAsync(db, cancellationToken);
    }

    private static async Task<Result> SeedCoreAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        bool alreadySeeded = await db.Users.AsNoTracking().AnyAsync(
            user => user.Email == TestMarkerEmail,
            cancellationToken);
        if (alreadySeeded)
            return new Result(0, 0, 0, 0, AlreadySeeded: true);

        List<Product> products = await db.Products
            .Where(product => product.IsActive && !product.IsDeleted)
            .OrderBy(product => product.Id)
            .ToListAsync(cancellationToken);

        if (products.Count == 0)
            return new Result(0, 0, 0, 0);

        // Exactly ceil(25%) of active products. Do not touch products that
        // already have any non-deleted customer review.
        int target = (products.Count + 3) / 4;
        HashSet<Guid> reviewedProductIds = (await db.Reviews.AsNoTracking()
            .Where(review => !review.IsDeleted)
            .Select(review => review.ProductId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        List<Product> selected = products
            .Where(product => !reviewedProductIds.Contains(product.Id))
            .Take(target)
            .ToList();
        if (selected.Count == 0)
            return new Result(0, 0, 0, 0);

        // Randomly generated passwords are not returned or logged. No
        // hardcoded credentials can be used to sign in as demo users.
        var passwordHasher = new PasswordHasher<User>();
        var users = new List<User>(DemoUserCount);
        for (int i = 0; i < DemoUserCount; i++)
        {
            User user = new()
            {
                Email = $"demo-review-{i + 1:D2}@{EmailDomain}",
                Names = $"Тестов профил {i + 1:D2} [ДЕМО]",
                Phone = "0000000000",
                PasswordHash = string.Empty,
                Role = Roles.RegisteredCustomer
            };
            string randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
            user.PasswordHash = passwordHasher.HashPassword(user, randomPassword);
            users.Add(user);
        }

        db.Users.AddRange(users);

        // Half of the selected products receive 2 or 3 reviews; remaining
        // products receive 1. Ratings cycle through all five values.
        int multipleCount = (selected.Count + 1) / 2;
        var reviews = new List<Review>();
        DateTime seedDate = DateTime.UtcNow.Date;
        for (int i = 0; i < selected.Count; i++)
        {
            Product product = selected[i];
            int reviewCount = i < multipleCount ? 2 + (i % 2) : 1;
            int starsTotal = 0;

            for (int j = 0; j < reviewCount; j++)
            {
                int rating = (i + j * 2) % 5 + 1;
                starsTotal += rating;
                User user = users[(i + j) % users.Count];
                var review = new Review
                {
                    UserId = user.Id,
                    ProductId = product.Id,
                    Rating = (byte)rating,
                    Content = "[ТЕСТОВО РЕВЮ — НЕ Е РЕАЛНО КЛИЕНТСКО МНЕНИЕ] " +
                              ReviewSamples[(i + j) % ReviewSamples.Length],
                    CreatedOn = seedDate.AddDays(-((i * 3 + j) % 26)).AddHours(10 + j)
                };
                reviews.Add(review);
            }

            product.Rating = Math.Round(
                (double)starsTotal / reviewCount, 2,
                MidpointRounding.AwayFromZero);
            product.ModifiedOn = DateTime.UtcNow;
        }

        db.Reviews.AddRange(reviews);
        await db.SaveChangesAsync(cancellationToken);
        return new Result(users.Count, selected.Count, reviews.Count, multipleCount);
    }
}
