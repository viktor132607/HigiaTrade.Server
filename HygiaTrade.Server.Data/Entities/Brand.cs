namespace HygiaTrade.Data.Entities;

public class Brand : GenericEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ThumbnailImageUrl { get; set; }
    public string? Description { get; set; }
}
