using System.ComponentModel.DataAnnotations;

namespace HygiaTrade.Common.Requests.OrderItem;

public class AddOrderItemRequest
{
    public required Guid ProductId { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
    public required int Quantity { get; set; }
}
