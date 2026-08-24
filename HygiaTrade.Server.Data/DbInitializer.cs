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

            // Production bootstrap only: keep required admin accounts available.
            // Catalogue, customers, orders, reviews, stock and discounts are real database data
            // and must never be recreated or resurrected by demo seeders on application startup.
            await UserSeeder.SeedAsync(db);
        }
    }
}
