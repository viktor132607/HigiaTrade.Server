using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HygiaTrade.API.Helpers;
using HygiaTrade.Common.Requests.Auth;
using HygiaTrade.Common.Requests.Users;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Controllers;

[Authorize(Roles = Roles.Admin)]
[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService, IAuthService authService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        return await ControllerProcessor.ProcessAsync(() => userService.GetAsync(), this);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        return await ControllerProcessor.ProcessAsync(() => userService.GetByIdAsync(id), this);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] RegisterUserRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => authService.RegisterAsync(request), this, true);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateUserRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => userService.UpdateAsync(request), this, true);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        return await ControllerProcessor.ProcessAsync<object>(
            async () => await userService.DeleteAsync(id), this);
    }
    
    [HttpPut("promote-to-admin")]
    public async Task<IActionResult> PromoteToAdmin([FromBody] RoleChangeRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => userService.PromoteToAdminAsync(request), this, true);
    }
    
    [HttpPut("demote-to-registered-customer")]
    public async Task<IActionResult> DemoteToRegisteredCustomer([FromBody] RoleChangeRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => userService.DemoteToRegisteredCustomerAsync(request), this, true);
    }
}
