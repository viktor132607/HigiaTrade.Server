using HygiaTrade.Common.Responses.Image;
using HygiaTrade.Core.StaticClasses;

namespace HygiaTrade.Common.Responses.Product;

public class ProductsResponse
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }
    
    public required string MainImageUrl { get; set; }
    public bool IsActive { get; set; }

    // Legacy aliases kept for the current frontend. Both are VAT-inclusive.
    public decimal RegularPrice { get; set; }
    public byte DiscountPercentage { get; set; }
    public decimal DiscountedPrice { get; set; }

    public decimal RetailPriceInclVat { get; set; }
    public decimal RetailPriceExclVat { get; set; }
    public decimal DiscountedPriceInclVat { get; set; }
    public decimal DiscountedPriceExclVat { get; set; }
    public decimal WholesalePriceInclVat { get; set; }
    public decimal WholesalePriceExclVat { get; set; }
    public uint WholesaleMinQuantity { get; set; }
    public decimal VatRate { get; set; }
    public bool WholesaleEnabled { get; set; }

    public string CurrencyCode { get; set; } = CurrencyDefaults.Code;
    public double Rating { get; set; } 

    public uint Quantity { get; set; }

    public Guid CategoryId { get; set; }
    
    public required string CategoryName { get; set; }
    
    public ICollection<ImageResponse> SecondaryImages { get; set; } = new List<ImageResponse>();
}
