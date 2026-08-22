using HygiaTrade.Core.StaticClasses;

namespace HygiaTrade.Common.Responses.OrderItem;

public class OrderItemResponse
{
    public required Guid ProductId { get; set; }

    // Legacy/current values remain VAT-inclusive.
    public required decimal SinglePrice { get; set; }
    public required decimal TotalPrice { get; set; }

    public decimal SinglePriceExclVat { get; set; }
    public decimal TotalPriceExclVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal VatRate { get; set; }
    public string PricingTier { get; set; } = "Retail";

    public string CurrencyCode { get; set; } = CurrencyDefaults.Code;
    public required int Quantity { get; set; }
    
    public required string Title { get; set; }
    public required string PrimaryImageUri { get; set; }
}
