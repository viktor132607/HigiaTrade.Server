using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Seed;

public static class ReviewSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (db.Reviews.Any())
        {
            return;
        }

        Dictionary<string, User> usersByEmail = db.Users.ToDictionary(user => user.Email, user => user);
        Dictionary<string, Product> productsByTitle = db.Products.ToDictionary(product => product.Title, product => product);
        DateTime now = DateTime.UtcNow;

        List<Review> reviews =
        [
            CreateReview(usersByEmail["martin.georgiev@hygiatrade.bg"], productsByTitle["Nike Air Zoom Pegasus 40"], 5, "Cushioning stays consistent on 10 km runs and the upper does not rub around the toes.", now.AddDays(-12)),
            CreateReview(usersByEmail["petya.stoyanova@hygiatrade.bg"], productsByTitle["Nike Air Zoom Pegasus 40"], 4, "Comfortable for daily mileage. I would size up half a number for thicker socks.", now.AddDays(-8)),
            CreateReview(usersByEmail["ivan.petrov@hygiatrade.bg"], productsByTitle["Garmin Forerunner 255 GPS Running Watch"], 5, "GPS lock is quick even in the city and the battery easily lasts through a training week.", now.AddDays(-7)),
            CreateReview(usersByEmail["teodora.marinova@hygiatrade.bg"], productsByTitle["Garmin Forerunner 255 GPS Running Watch"], 4, "Easy to read on outdoor runs and the recovery metrics are actually useful.", now.AddDays(-3)),
            CreateReview(usersByEmail["maria.dimitrova@hygiatrade.bg"], productsByTitle["Adidas Predator Accuracy.1 FG Football Boots"], 5, "Secure fit through the midfoot and very clean contact when striking the ball.", now.AddDays(-9)),
            CreateReview(usersByEmail["georgi.kolev@hygiatrade.bg"], productsByTitle["Adidas Predator Accuracy.1 FG Football Boots"], 4, "Good traction on dry grass. The boot needs one or two sessions to break in.", now.AddDays(-4)),
            CreateReview(usersByEmail["daniel.nikolov@hygiatrade.bg"], productsByTitle["PUMA Orbita 2 TB Football"], 4, "Keeps its shape after weekly five-a-side sessions and the surface is easy to wipe clean.", now.AddDays(-10)),
            CreateReview(usersByEmail["stela.atanasova@hygiatrade.bg"], productsByTitle["Under Armour HeatGear Training T-Shirt"], 5, "Light fabric, dries fast, and still looks good after repeated washes.", now.AddDays(-6)),
            CreateReview(usersByEmail["borislava.ilieva@hygiatrade.bg"], productsByTitle["Bowflex SelectTech 552 Adjustable Dumbbells"], 5, "Very practical for a home gym. Weight changes are quick enough for supersets.", now.AddDays(-11)),
            CreateReview(usersByEmail["martin.georgiev@hygiatrade.bg"], productsByTitle["Bowflex SelectTech 552 Adjustable Dumbbells"], 5, "Solid mechanism and no need for a separate rack of dumbbells.", now.AddDays(-5)),
            CreateReview(usersByEmail["nikol.todorov@hygiatrade.bg"], productsByTitle["Salomon X Ultra 4 GTX Hiking Shoes"], 5, "Stable on wet forest paths and the waterproof lining held up in steady spring rain.", now.AddDays(-13)),
            CreateReview(usersByEmail["petya.stoyanova@hygiatrade.bg"], productsByTitle["Coleman Darwin 3 Tent"], 4, "Straightforward setup and enough room for two adults plus bags.", now.AddDays(-14)),
            CreateReview(usersByEmail["georgi.kolev@hygiatrade.bg"], productsByTitle["Giro Register MIPS Cycling Helmet"], 4, "Ventilation is good for longer rides and the dial fit system is easy to adjust.", now.AddDays(-2)),
            CreateReview(usersByEmail["daniel.nikolov@hygiatrade.bg"], productsByTitle["Wilson Pro Staff RF97 v13 Tennis Racket"], 5, "Excellent control on flat backhands. Best suited to players who like a heavier frame.", now.AddDays(-15)),
            CreateReview(usersByEmail["maria.dimitrova@hygiatrade.bg"], productsByTitle["HEAD Tour XT Tennis Balls 4 Pack"], 4, "Consistent bounce for club sessions and the felt lasts longer than cheaper tubes.", now.AddDays(-7)),
            CreateReview(usersByEmail["ivan.petrov@hygiatrade.bg"], productsByTitle["Wilson Evolution Indoor Game Basketball"], 5, "Soft grip and reliable bounce on indoor courts. Worth the premium over entry models.", now.AddDays(-4)),
            CreateReview(usersByEmail["stela.atanasova@hygiatrade.bg"], productsByTitle["Spalding Slam Jam Basketball Rim"], 4, "Sturdy enough for a home setup and the mounting hardware felt solid.", now.AddDays(-6)),
            CreateReview(usersByEmail["borislava.ilieva@hygiatrade.bg"], productsByTitle["YETI Rambler 26 oz Water Bottle"], 5, "Keeps water cold through a full workday and fits well in a gym bag side pocket.", now.AddDays(-1)),
        ];

        await db.Reviews.AddRangeAsync(reviews);
        await db.SaveChangesAsync();

        foreach (IGrouping<Guid, Review> reviewGroup in db.Reviews.GroupBy(review => review.ProductId))
        {
            Product? product = await db.Products.FindAsync(reviewGroup.Key);
            if (product is null)
            {
                continue;
            }

            product.Rating = Math.Round(reviewGroup.Average(review => review.Rating), 1);
        }

        await db.SaveChangesAsync();
    }

    private static Review CreateReview(User user, Product product, byte rating, string content, DateTime createdOn)
    {
        return new Review
        {
            UserId = user.Id,
            ProductId = product.Id,
            Content = content,
            Rating = rating,
            CreatedOn = createdOn,
            ModifiedOn = createdOn,
        };
    }
}
