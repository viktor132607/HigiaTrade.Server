using HygiaTrade.Data.Entities;

namespace HygiaTrade.Data.Seed;

public static class ProductPackagingFactory
{
    // Initial packaging suggestions are drafts; only the base product has existing inventory.
    public static List<ProductPackagingOption> Create(Product product)
    {
        bool sano = product.Title.Contains("Sano Oxygen Baby Gel", StringComparison.OrdinalIgnoreCase);
        string[] labels = sano
            ? ["1 л", "3 л", "Кашон 12 × 1 л", "Кашон 20 × 3 л"]
            : ["Единична опаковка", "Опаковка 3 бр.", "Кашон 12 бр.", "Кашон 20 бр."];
        int[] sizes = [1, 3, 12, 60];
        return Enumerable.Range(0, labels.Length).Select(i => new ProductPackagingOption
        {
            ProductId = product.Id,
            Label = labels[i],
            UnitsPerPackage = sizes[i],
            PriceInclVat = decimal.Round(product.RegularPrice * sizes[i], 2),
            StockQuantity = 0,
            IsDefault = i == 0,
            SortOrder = i
        }).ToList();
    }
}
