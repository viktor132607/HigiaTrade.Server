using HygiaTrade.Core.Enums;

namespace HygiaTrade.Data.Entities
{
    public class Order : GenericEntity
    {
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public string? GuestEmail { get; set; }
        public string? Names { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? DeliveryRegion { get; set; }
        public string? PaymentMethod { get; set; }
        public string? DeliveryMethod { get; set; }
        public bool InvoiceRequested { get; set; }
        public string? InvoiceCompanyName { get; set; }
        public string? InvoiceCompanyId { get; set; }
        public string? InvoiceVatId { get; set; }
        public string? InvoiceAddress { get; set; }


        public string PaymentStatus { get; set; } = "Unpaid";
        public Guid? StripeCheckoutAttemptId { get; set; }
        public string? StripeCheckoutFingerprint { get; set; }
        public string? StripeSessionId { get; set; }
        public string? StripeCheckoutUrl { get; set; }
        public DateTime? StripeExpiresAt { get; set; }
        public bool StripeLiveMode { get; set; }

        public decimal OrderSubtotalExclVat { get; set; }
        public decimal OrderVatAmount { get; set; }
        public decimal OrderTotalPrice { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Created;
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
