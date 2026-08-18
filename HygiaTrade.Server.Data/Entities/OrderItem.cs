namespace HygiaTrade.Data.Entities
{
    public class OrderItem : GenericEntity
    {
        public Guid OrderId { get; set; }

        public Order Order { get; set; } = null!;

        public Guid ProductId { get; set; }

        public Product Product { get; set; } = null!;

        public required int Quantity { get; set; }

        public required decimal SinglePrice { get; set; }
        public required decimal TotalPrice { get; set; }
        public required string Title { get; set; }
        public required string PrimaryImageUri { get; set; }

    }
}
