using System.ComponentModel.DataAnnotations;

namespace HygiaTrade.Common.Requests.Auth;

public class RefreshTokenRequest
{
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    public required string RefreshToken { get; set; }
}
