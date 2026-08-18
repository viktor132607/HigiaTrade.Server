using System.ComponentModel.DataAnnotations;

namespace HygiaTrade.Common.Requests.Image;

public class CreateImageRequest
{
    [Required]
    public required string Uri { get; set; }
}
