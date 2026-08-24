using HygiaTrade.Common.Requests.Image;

namespace HygiaTrade.Common.Requests.Product;

public class UpdateProductRequest
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public required string MainImageUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // Retail prices are VAT-inclusive to stay compatible with the current storefront.
    public decimal RegularPrice { get; set; }
    public byte DiscountPercentage { get; set; }
    public decimal DiscountedPrice { get; set; }

    // Nullable for backwards compatibility with the current frontend. Omitted values preserve existing settings.
    public decimal? WholesalePrice { get; set; }
    public uint? WholesaleMinQuantity { get; set; }
    public decimal? VatRate { get; set; }

    public uint Quantity { get; set; }

    public Guid CategoryId { get; set; }
    
    public ICollection<UpdateImageRequest> SecondaryImages { get; set; } = new List<UpdateImageRequest>();
}
