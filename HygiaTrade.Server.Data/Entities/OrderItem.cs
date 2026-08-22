using HygiaTrade.Core.Enums;

namespace HygiaTrade.Data.Entities
{
    public class OrderItem : GenericEntity
    {
        public Guid OrderId { get; set; }

        public Order Order { get; set; } = null!;

        public Guid ProductId { get; set; }

        public Product Product { get; set; } = null!;

        public required int Quantity { get; set; }

        // Legacy/current totals remain VAT-inclusive so the existing frontend keeps working.
        public required decimal SinglePrice { get; set; }
        public required decimal TotalPrice { get; set; }

        // Tax and pricing snapshots preserve the exact commercial terms used for the order line.
        public decimal SinglePriceExclVat { get; set; }
        public decimal TotalPriceExclVat { get; set; }
        public decimal VatAmount { get; set; }
        public decimal VatRate { get; set; } = 20m;
        public PricingTier PricingTier { get; set; } = PricingTier.Retail;

        public required string Title { get; set; }
        public required string PrimaryImageUri { get; set; }
    }
}
