using System.ComponentModel.DataAnnotations;
namespace HygiaTrade.Common.Requests.Order;

public class StripeCheckoutRequest : GuestOrderRequest
{
    public Guid CheckoutAttemptId { get; set; }
    [Range(50, 1000000)] public decimal ExpectedTotal { get; set; }
}
