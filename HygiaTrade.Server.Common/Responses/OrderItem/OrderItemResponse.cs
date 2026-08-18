using HygiaTrade.Core.StaticClasses;

namespace HygiaTrade.Common.Responses.OrderItem;

public class OrderItemResponse
{
    public required Guid ProductId { get; set; }
    public required decimal SinglePrice { get; set; }
    public required decimal TotalPrice { get; set; }
    public string CurrencyCode { get; set; } = CurrencyDefaults.Code;
    public required int Quantity { get; set; }
    
    public required string Title { get; set; }
    public required string PrimaryImageUri { get; set; }

}
