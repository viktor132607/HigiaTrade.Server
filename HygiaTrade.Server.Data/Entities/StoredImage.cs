namespace HygiaTrade.Data.Entities;

public class StoredImage : GenericEntity
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required byte[] Data { get; set; }
}
