using System.ComponentModel.DataAnnotations;

namespace HygiaTrade.Common.Requests.Category;

public class CreateCategoryRequest
{
    [Required]
    public required string Name { get; set; }

    public string? ImageURI { get; set; }
}
