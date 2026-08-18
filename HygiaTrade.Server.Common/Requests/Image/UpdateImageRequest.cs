using System.ComponentModel.DataAnnotations;

namespace HygiaTrade.Common.Requests.Image;

public class UpdateImageRequest
{
    public Guid? Id { get; set; }
    
    [Required]
    public required string Uri { get; set; }
}
