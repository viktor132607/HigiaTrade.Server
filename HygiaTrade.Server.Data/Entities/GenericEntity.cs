namespace HygiaTrade.Data.Entities;

public class GenericEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedOn { get; init; } = DateTime.UtcNow;
    public DateTime ModifiedOn { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}
