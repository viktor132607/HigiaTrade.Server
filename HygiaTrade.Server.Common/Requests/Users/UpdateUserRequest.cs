using System.ComponentModel.DataAnnotations;

namespace HygiaTrade.Common.Requests.Users;

public class UpdateUserRequest
{
    [Required]
    public Guid Id { get; set; } 
    
    [Required]
    public required string Email { get; set; } 
    
    [Required]
    public required string Names { get; set; } 
    
    [Required]
    public required string Phone { get; set; } 
}
