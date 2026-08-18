using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HygiaTrade.API.Helpers;
using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Common.Requests.OrderItem;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        return await ControllerProcessor.ProcessAsync(() => orderService.GetAsync(), this);
    }
    
    [HttpPut]
    public async Task<IActionResult> AddProductAsync([FromBody] AddOrderItemRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => orderService.AddProductAsync(request), this, true);
    }
    
    [HttpDelete]
    public async Task<IActionResult> RemoveProductAsync([FromBody] RemoveOrderItemRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => orderService.RemoveProductAsync(request), this, true);
    }
    
    [HttpPost]
    public async Task<IActionResult> SendOrder([FromBody] SendOrderRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => orderService.SendCurrentAsync(request), this);
    }
    
    [HttpGet("get-list")]
    public async Task<IActionResult> SearchOrdersAsync([FromQuery] SearchOrderRequest? request)
    {
        return await ControllerProcessor.ProcessAsync(
            () => orderService.SearchOrdersAsync(request ?? new SearchOrderRequest()),
            this,
            true);
    }
    
    [HttpPut("change-status")]
    public async Task<IActionResult> AddProductAsync([FromBody] ChangeOrderStatusRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => orderService.ChangeStatusAsync(request), this, true);
    }
}
