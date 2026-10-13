namespace HygiaTrade.Data.Entities;

public sealed class ProductPackagingOption : GenericEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Label { get; set; } = string.Empty;
    public int UnitsPerPackage { get; set; } = 1;
    public decimal PriceInclVat { get; set; }
    public uint StockQuantity { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
