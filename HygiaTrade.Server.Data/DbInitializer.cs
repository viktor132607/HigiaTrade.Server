using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HygiaTrade.Data.Seed;

namespace HygiaTrade.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            IServiceScopeFactory scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            using IServiceScope scope = scopeFactory.CreateScope();

            ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await RecoverCatalogueIfNeededAsync(db);

            await UserSeeder.SeedAsync(db);
            await CategorySeeder.SeedAsync(db);
            await ProductSeeder.SeedAsync(db);

            // Legacy seeders reference products that are no longer part of the current catalogue.
            // Keep them disabled until their seed data is aligned with ProductSeeder.
            // await ReviewSeeder.SeedAsync(db);
            // await OrderSeeder.SeedAsync(db);
            // await WishlistSeeder.SeedAsync(db);
        }

        private static async Task RecoverCatalogueIfNeededAsync(ApplicationDbContext db)
        {
            bool hasAnyCategories = await db.Categories.AnyAsync();
            bool hasActiveCategories = await db.Categories.AnyAsync(category => !category.IsDeleted);

            if (hasAnyCategories && !hasActiveCategories)
            {
                await db.Categories
                    .Where(category => category.IsDeleted)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(category => category.IsDeleted, false)
                        .SetProperty(category => category.ModifiedOn, DateTime.UtcNow));
            }

            bool hasAnyProducts = await db.Products.AnyAsync();
            bool hasActiveProducts = await db.Products.AnyAsync(product => !product.IsDeleted);

            if (hasAnyProducts && !hasActiveProducts)
            {
                await db.Products
                    .Where(product => product.IsDeleted)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(product => product.IsDeleted, false)
                        .SetProperty(product => product.ModifiedOn, DateTime.UtcNow));
            }

            User? seedAdmin = await db.Users
                .FirstOrDefaultAsync(user => user.Email == "admin@hygiatrade.bg");

            if (seedAdmin is not null && seedAdmin.IsDeleted)
            {
                seedAdmin.IsDeleted = false;
                seedAdmin.ModifiedOn = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        }
    }
}
