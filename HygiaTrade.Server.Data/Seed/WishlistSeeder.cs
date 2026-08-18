using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Seed;

public static class WishlistSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (db.WishlistItems.Any())
        {
            return;
        }

        Dictionary<string, User> usersByEmail = db.Users.ToDictionary(user => user.Email, user => user);
        Dictionary<string, Product> productsByTitle = db.Products.ToDictionary(product => product.Title, product => product);

        WishlistItem[] wishlistItems =
        [
            CreateWishlistItem(usersByEmail["martin.georgiev@hygiatrade.bg"], productsByTitle["Garmin Forerunner 255 GPS Running Watch"]),
            CreateWishlistItem(usersByEmail["maria.dimitrova@hygiatrade.bg"], productsByTitle["Wilson Pro Staff RF97 v13 Tennis Racket"]),
            CreateWishlistItem(usersByEmail["ivan.petrov@hygiatrade.bg"], productsByTitle["Bowflex SelectTech 552 Adjustable Dumbbells"]),
            CreateWishlistItem(usersByEmail["petya.stoyanova@hygiatrade.bg"], productsByTitle["Coleman Darwin 3 Tent"]),
            CreateWishlistItem(usersByEmail["georgi.kolev@hygiatrade.bg"], productsByTitle["Adidas Predator Accuracy.1 FG Football Boots"]),
            CreateWishlistItem(usersByEmail["stela.atanasova@hygiatrade.bg"], productsByTitle["YETI Rambler 26 oz Water Bottle"]),
        ];

        await db.WishlistItems.AddRangeAsync(wishlistItems);
        await db.SaveChangesAsync();
    }

    private static WishlistItem CreateWishlistItem(User user, Product product)
    {
        return new WishlistItem
        {
            UserId = user.Id,
            ProductId = product.Id,
        };
    }
}
