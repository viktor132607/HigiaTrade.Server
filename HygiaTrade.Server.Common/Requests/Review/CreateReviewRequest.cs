using System.ComponentModel.DataAnnotations;

namespace HygiaTrade.Common.Requests.Review;

public class CreateReviewRequest
{
    [Required]
    public required Guid ProductId { get; set; }

    public string? Content { get; set; }

    [Range(1, 5)]
    public required byte Rating { get; set; }
}
