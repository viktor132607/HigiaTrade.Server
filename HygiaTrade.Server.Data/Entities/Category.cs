namespace HygiaTrade.Data.Entities
{
    public class Category : GenericEntity
    {
        public required string Name { get; set; }
        public required string? ImageUri { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }
        public ICollection<Category> Subcategories { get; set; } = new List<Category>();
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
