using HygiaTrade.API.Models;
using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public sealed class InvoiceImportController(
    IInvoiceImportService invoiceImportService) : ControllerBase
{
    [HttpPost("extract")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> ExtractAsync(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        try
        {
            ExtractInvoiceResponse response =
                await invoiceImportService.ExtractAsync(file, cancellationToken);

            return Ok(response);
        }
        catch (InvoiceImportServiceException exception)
        {
            return StatusCode(
                exception.StatusCode,
                new { message = exception.Message });
        }
        catch (FileNotFoundException exception)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = exception.Message });
        }
        catch (TimeoutException exception)
        {
            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                new { message = exception.Message });
        }
    }

    [HttpPost("commit")]
    public async Task<IActionResult> CommitAsync(
        [FromBody] ImportInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            ImportInvoiceResponse response =
                await invoiceImportService.CommitAsync(request, cancellationToken);

            return Ok(response);
        }
        catch (InvoiceImportServiceException exception)
        {
            return StatusCode(
                exception.StatusCode,
                new { message = exception.Message });
        }
    }
}
