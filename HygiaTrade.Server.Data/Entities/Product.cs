namespace HygiaTrade.Data.Entities
{
    public class Product : GenericEntity
    {
        public string Title { get; set; }
        public string Description { get; set; } 
        public string MainImageUrl { get; set; }

        // Consumer-facing retail prices are stored VAT-inclusive.
        public decimal RegularPrice { get; set; }
        public byte DiscountPercentage { get; set; }
        public decimal DiscountedPrice { get; set; }

        // B2B wholesale price is also stored VAT-inclusive. A value of 0 disables wholesale pricing.
        public decimal WholesalePrice { get; set; }
        public uint WholesaleMinQuantity { get; set; }

        // VAT is stored per product so the pricing model can support different tax rates later.
        public decimal VatRate { get; set; } = 20m;

        public uint Quantity { get; set; }
        public double Rating { get; set; } = 0;
        public Guid CategoryId { get; set; }

        public Category Category { get; set; }

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Image> SecondaryImages { get; set; } = new List<Image>();
    }
}
