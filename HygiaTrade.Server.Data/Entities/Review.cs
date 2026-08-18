namespace HygiaTrade.Data.Entities
{
    public class Review : GenericEntity
    {
        public Guid UserId { get; set; }

        public User User { get; set; } = null!;

        public Guid ProductId { get; set; }

        public Product Product { get; set; } = null!;

        public string Content { get; set; } = null!;

        public byte Rating { get; set; }
    }
}
