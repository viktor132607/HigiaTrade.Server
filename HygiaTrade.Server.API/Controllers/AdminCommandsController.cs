using System.ComponentModel.DataAnnotations;
using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/admin/commands")]
public sealed class AdminCommandsController(IAdminCommandService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Execute(AdminCommandRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.ExecuteAsync(request.Command, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}

public sealed record AdminCommandRequest([property: Required, StringLength(200)] string Command);
