using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/InvoiceImport")]
public sealed class InvoiceNumberValidationController(
    IInvoiceNumberService invoiceNumberService) : ControllerBase
{
    [HttpGet("check-number")]
    public async Task<IActionResult> CheckInvoiceNumberAsync(
        [FromQuery] string invoiceNumber,
        CancellationToken cancellationToken)
    {
        try
        {
            InvoiceNumberStatus response =
                await invoiceNumberService.CheckAsync(
                    invoiceNumber,
                    cancellationToken);

            return Ok(new { exists = response.Exists });
        }
        catch (InvoiceNumberServiceException exception)
        {
            return StatusCode(
                exception.StatusCode,
                new { message = exception.Message });
        }
    }
}
