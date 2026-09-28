using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/product-import")]
public sealed class ProductImportController(
    IProductExcelImportService importService) : ControllerBase
{
    [HttpPost("excel")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportExcelAsync(
        IFormFile? file,
        [FromQuery] bool updateExisting = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ProductExcelImportResult result =
                await importService.ImportAsync(
                    file,
                    updateExisting,
                    cancellationToken);

            return Ok(result);
        }
        catch (ProductExcelImportException exception)
        {
            return StatusCode(
                exception.StatusCode,
                new { message = exception.Message });
        }
    }
}
