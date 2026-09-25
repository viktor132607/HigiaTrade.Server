using System.ComponentModel.DataAnnotations;
namespace HygiaTrade.Common.Requests.Order;

public class CheckoutDetails
{
    [Required, StringLength(120)] public required string Names { get; set; }
    [Required, RegularExpression(@"\d{4}")] public required string PostalCode { get; set; }
    [Required] public required string Country { get; set; }
    [Required, StringLength(100)] public required string City { get; set; }
    [Required, StringLength(300)] public required string Address { get; set; }
    [Required, Phone, StringLength(30)] public required string Phone { get; set; }
    public string? DeliveryRegion { get; set; }
    public string? PaymentMethod { get; set; }
    public string? DeliveryMethod { get; set; }
    public bool ConsentAccepted { get; set; }
    public bool InvoiceRequested { get; set; }
    [StringLength(200)] public string? InvoiceCompanyName { get; set; }
    [StringLength(13)] public string? InvoiceCompanyId { get; set; }
    [StringLength(15)] public string? InvoiceVatId { get; set; }
    [StringLength(300)] public string? InvoiceAddress { get; set; }
}
