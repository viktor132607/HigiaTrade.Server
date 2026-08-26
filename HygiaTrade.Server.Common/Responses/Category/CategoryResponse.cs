namespace HygiaTrade.Common.Responses.Category;

public class CategoryResponse
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public string? ImageURI { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
}
