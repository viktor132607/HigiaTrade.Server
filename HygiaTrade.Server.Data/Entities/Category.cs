namespace HygiaTrade.Data.Entities
{
    public class Category : GenericEntity
    {
        public required string Name { get; set; }
        public required string? ImageUri { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
