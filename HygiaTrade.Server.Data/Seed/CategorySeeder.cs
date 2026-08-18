using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Seed;

public static class CategorySeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        Category[] categories =
        [
            new()
            {
                Name = "Перилни препарати",
                ImageUri = "https://images.unsplash.com/photo-1610557892470-55d9e80c0bce?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Течни и прахообразни перилни препарати",
                ImageUri = "https://images.unsplash.com/photo-1583947215259-38e31be8751f?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Омекотители",
                ImageUri = "https://images.unsplash.com/photo-1626806787461-102c1bfaaea1?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Препарати за петна",
                ImageUri = "https://images.unsplash.com/photo-1581578731548-c64695cc6952?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Бебешки перилни препарати и омекотители",
                ImageUri = "https://images.unsplash.com/photo-1515488042361-ee00e0ddd4e4?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Добавки за пране",
                ImageUri = "https://images.unsplash.com/photo-1604335399105-a0c585fd81a1?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Препарати за миене на съдове",
                ImageUri = "https://images.unsplash.com/photo-1609501676725-7186f017a4b7?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Почистващи препарати",
                ImageUri = "https://images.unsplash.com/photo-1563453392212-326f5e854473?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Препарати с белина",
                ImageUri = "https://images.unsplash.com/photo-1585421514284-efb74c2b69ba?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Препарати за общо почистване",
                ImageUri = "https://images.unsplash.com/photo-1584464491033-06628f3a6b7b?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Почистващи препарати за кухня",
                ImageUri = "https://images.unsplash.com/photo-1556911220-bff31c812dba?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Почистващи препарати за баня",
                ImageUri = "https://images.unsplash.com/photo-1584622650111-993a426fbf0a?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Почистващи препарати за подови настилки",
                ImageUri = "https://images.unsplash.com/photo-1527515637462-cff94eecc1ac?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Почистващи препарати за мебели",
                ImageUri = "https://images.unsplash.com/photo-1555041469-a586c61ea9bc?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Почистващи препарати за прозорци",
                ImageUri = "https://images.unsplash.com/photo-1527689368864-3a821dbccc34?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Почистващи препарати за килими и дамаски",
                ImageUri = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Аксесоари",
                ImageUri = "https://images.unsplash.com/photo-1528740561666-dc2479dc08ab?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Професионално почистване",
                ImageUri = "https://images.unsplash.com/photo-1581578731548-c64695cc6952?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Ароматизатори за въздух",
                ImageUri = "https://images.unsplash.com/photo-1608571423902-eed4a5ad8108?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Инсектициди",
                ImageUri = "https://images.unsplash.com/photo-1584464491033-06628f3a6b7b?auto=format&fit=crop&w=1200&q=80",
            },
            new()
            {
                Name = "Хартия",
                ImageUri = "https://images.unsplash.com/photo-1584556812952-905ffd0c611a?auto=format&fit=crop&w=1200&q=80",
            },
        ];

        HashSet<string> existingCategoryNames = db.Categories
            .Where(category => !string.IsNullOrWhiteSpace(category.Name))
            .Select(category => category.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Category[] categoriesToAdd = categories
            .Where(category => !existingCategoryNames.Contains(category.Name))
            .ToArray();

        if (categoriesToAdd.Length == 0)
        {
            return;
        }

        db.Categories.AddRange(categoriesToAdd);
        await db.SaveChangesAsync();
    }
}
