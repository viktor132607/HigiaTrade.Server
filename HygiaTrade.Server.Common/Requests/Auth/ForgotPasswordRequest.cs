using System.ComponentModel.DataAnnotations;

namespace HygiaTrade.Common.Requests.Auth;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}
