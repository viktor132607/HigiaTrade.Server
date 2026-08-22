using HygiaTrade.Core.StaticClasses;

namespace HygiaTrade.Common.Responses.Product;

public class ProductPriceQuoteResponse
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string PricingTier { get; set; } = "Retail";
    public uint WholesaleMinQuantity { get; set; }
    public decimal VatRate { get; set; }

    public decimal UnitPriceExclVat { get; set; }
    public decimal UnitPriceInclVat { get; set; }
    public decimal TotalExclVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }

    public string CurrencyCode { get; set; } = CurrencyDefaults.Code;
}
