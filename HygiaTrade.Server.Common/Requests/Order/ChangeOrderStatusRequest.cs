using System.ComponentModel.DataAnnotations;
using HygiaTrade.Core.Enums;

namespace HygiaTrade.Common.Requests.Order;

public class ChangeOrderStatusRequest
{
    [Required]
    public required Guid OrderId { get; set; }
    
    [Required]
    public required OrderStatus OrderStatus { get; set; }
}
