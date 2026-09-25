using System.ComponentModel.DataAnnotations;

namespace HygiaTrade.Common.Requests.Order;

public class GuestOrderRequest : CheckoutDetails
{
    [Required, EmailAddress] public required string Email { get; set; }
    [Required, MinLength(1)] public required List<GuestOrderItemRequest> Items { get; set; }
}

public class GuestOrderItemRequest
{
    public Guid ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
}
