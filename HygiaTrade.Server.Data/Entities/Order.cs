using HygiaTrade.Core.Enums;

namespace HygiaTrade.Data.Entities
{
    public class Order : GenericEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; }
        public string? Names { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }

        // OrderTotalPrice remains the VAT-inclusive grand total for backward compatibility.
        public decimal OrderSubtotalExclVat { get; set; }
        public decimal OrderVatAmount { get; set; }
        public decimal OrderTotalPrice { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Created;
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
