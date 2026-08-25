using System.ComponentModel.DataAnnotations;

namespace HygiaTrade.Common.Requests.Order;

public class GuestOrderRequest
{
    [Required] public required string Names { get; set; }
    [Required, EmailAddress] public required string Email { get; set; }
    [Required] public required string PostalCode { get; set; }
    [Required] public required string Country { get; set; }
    [Required] public required string City { get; set; }
    [Required] public required string Address { get; set; }
    [Required] public required string Phone { get; set; }
    public string? PaymentMethod { get; set; }
    public string? DeliveryMethod { get; set; }
    public bool ConsentAccepted { get; set; }
    [Required, MinLength(1)] public required List<GuestOrderItemRequest> Items { get; set; }
}

public class GuestOrderItemRequest
{
    public Guid ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
}
