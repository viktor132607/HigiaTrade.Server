using System.ComponentModel.DataAnnotations;

namespace HygiaTrade.Common.Requests.Users;

public class RoleChangeRequest
{
    [Required]
    public required Guid UserId { get; set; }
}
