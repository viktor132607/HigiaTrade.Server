using HygiaTrade.API.Models;
using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public sealed class ReportsController(
    IReportsService reportsService,
    ILogger<ReportsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] int lowStockThreshold = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            AdminReportResponse report =
                await reportsService.GetAsync(
                    from,
                    to,
                    lowStockThreshold,
                    cancellationToken);

            return Ok(report);
        }
        catch (ReportsServiceException exception)
        {
            return StatusCode(
                exception.StatusCode,
                new { message = exception.Message });
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to generate admin report.");

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Справката не можа да бъде заредена. Провери логовете на сървъра."
                });
        }
    }
}
