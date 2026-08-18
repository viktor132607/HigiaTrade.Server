using HygiaTrade.Core.Enums;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Seed;

public static class OrderSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        if (db.Orders.Any())
        {
            return;
        }

        Dictionary<string, User> usersByEmail = db.Users.ToDictionary(user => user.Email, user => user);
        Dictionary<string, Product> productsByTitle = db.Products.ToDictionary(product => product.Title, product => product);
        DateTime today = DateTime.UtcNow.Date;

        List<SeedOrder> orders =
        [
            CreateOrder(usersByEmail["martin.georgiev@hygiatrade.bg"], OrderStatus.PendingVerification, today.AddHours(8).AddMinutes(40), "7000", "Ruse", "12 Borisova Str.", ("Nike Air Zoom Pegasus 40", 1), ("Under Armour HeatGear Training T-Shirt", 2)),
            CreateOrder(usersByEmail["maria.dimitrova@hygiatrade.bg"], OrderStatus.Verified, today.AddHours(10).AddMinutes(15), "4000", "Plovdiv", "24 Kapitan Raycho St.", ("Adidas Predator Accuracy.1 FG Football Boots", 1), ("PUMA Orbita 2 TB Football", 1)),
            CreateOrder(usersByEmail["ivan.petrov@hygiatrade.bg"], OrderStatus.Processing, today.AddHours(13).AddMinutes(5), "9000", "Varna", "8 Tsar Osvoboditel Blvd.", ("Garmin Forerunner 255 GPS Running Watch", 1), ("YETI Rambler 26 oz Water Bottle", 1)),
            CreateOrder(usersByEmail["petya.stoyanova@hygiatrade.bg"], OrderStatus.Shipped, today.AddDays(-1).AddHours(11).AddMinutes(25), "1000", "Sofia", "31 Cherni Vrah Blvd.", ("Salomon X Ultra 4 GTX Hiking Shoes", 1), ("YETI Rambler 26 oz Water Bottle", 1)),
            CreateOrder(usersByEmail["georgi.kolev@hygiatrade.bg"], OrderStatus.Delivered, today.AddDays(-1).AddHours(15).AddMinutes(10), "4023", "Plovdiv", "18 Svoboda Blvd.", ("Wilson Evolution Indoor Game Basketball", 2)),
            CreateOrder(usersByEmail["nikol.todorov@hygiatrade.bg"], OrderStatus.Delivered, today.AddDays(-3).AddHours(9).AddMinutes(50), "5000", "Veliko Tarnovo", "6 Vasil Levski St.", ("Bowflex SelectTech 552 Adjustable Dumbbells", 1)),
            CreateOrder(usersByEmail["borislava.ilieva@hygiatrade.bg"], OrderStatus.Delivered, today.AddDays(-4).AddHours(16).AddMinutes(20), "8230", "Nessebar", "44 Han Krum St.", ("Coleman Darwin 3 Tent", 1), ("YETI Rambler 26 oz Water Bottle", 2)),
            CreateOrder(usersByEmail["daniel.nikolov@hygiatrade.bg"], OrderStatus.Processing, today.AddDays(-5).AddHours(14).AddMinutes(45), "6000", "Stara Zagora", "15 General Stoletov St.", ("Wilson Pro Staff RF97 v13 Tennis Racket", 1), ("HEAD Tour XT Tennis Balls 4 Pack", 3)),
            CreateOrder(usersByEmail["teodora.marinova@hygiatrade.bg"], OrderStatus.Cancelled, today.AddDays(-6).AddHours(12).AddMinutes(15), "2700", "Blagoevgrad", "9 Makedonia Sq.", ("Giro Register MIPS Cycling Helmet", 1), ("Under Armour HeatGear Training T-Shirt", 1)),
            CreateOrder(usersByEmail["stela.atanasova@hygiatrade.bg"], OrderStatus.Delivered, today.AddDays(-8).AddHours(10).AddMinutes(35), "2300", "Pernik", "5 Krakra Blvd.", ("Spalding Slam Jam Basketball Rim", 1), ("Wilson Evolution Indoor Game Basketball", 1)),
            CreateOrder(usersByEmail["martin.georgiev@hygiatrade.bg"], OrderStatus.Delivered, today.AddDays(-10).AddHours(18).AddMinutes(5), "7000", "Ruse", "12 Borisova Str.", ("Nike Air Zoom Pegasus 40", 1)),
            CreateOrder(usersByEmail["maria.dimitrova@hygiatrade.bg"], OrderStatus.Delivered, today.AddDays(-14).AddHours(17).AddMinutes(40), "4000", "Plovdiv", "24 Kapitan Raycho St.", ("PUMA Orbita 2 TB Football", 2), ("HEAD Tour XT Tennis Balls 4 Pack", 4)),
        ];

        foreach (SeedOrder order in orders)
        {
            foreach ((string title, int quantity) in order.PendingItems)
            {
                Product product = productsByTitle[title];
                order.Items.Add(CreateOrderItem(order, product, quantity, order.CreatedOn));
            }

            order.OrderTotalPrice = order.Items.Sum(item => item.TotalPrice);
        }

        await db.Orders.AddRangeAsync(orders);
        await db.SaveChangesAsync();
    }

    private static SeedOrder CreateOrder(
        User user,
        OrderStatus status,
        DateTime createdOn,
        string postalCode,
        string city,
        string address,
        params (string title, int quantity)[] items)
    {
        return new SeedOrder
        {
            UserId = user.Id,
            Names = user.Names,
            PostalCode = postalCode,
            Country = "Bulgaria",
            City = city,
            Address = address,
            Phone = user.Phone,
            Status = status,
            CreatedOn = createdOn,
            ModifiedOn = createdOn,
            PendingItems = items,
        };
    }

    private static OrderItem CreateOrderItem(Order order, Product product, int quantity, DateTime createdOn)
    {
        decimal unitPrice = product.DiscountedPrice > 0 ? product.DiscountedPrice : product.RegularPrice;

        return new OrderItem
        {
            Order = order,
            ProductId = product.Id,
            Product = product,
            Quantity = quantity,
            SinglePrice = unitPrice,
            TotalPrice = unitPrice * quantity,
            Title = product.Title,
            PrimaryImageUri = product.MainImageUrl,
            CreatedOn = createdOn,
            ModifiedOn = createdOn,
        };
    }

    private sealed class SeedOrder : Order
    {
        public IEnumerable<(string title, int quantity)> PendingItems { get; init; } = [];
    }
}
