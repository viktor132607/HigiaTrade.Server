namespace HygiaTrade.Data.Entities;

public class Image : GenericEntity
{
    public required string Uri { get; set; }
    
    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }
}
