using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewProductsController(
    INewProductsService newProductsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 100)
    {
        NewProductsPageResponse response =
            await newProductsService.GetAsync(
                pageNumber,
                pageSize);

        return Ok(response);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("status/{productId:guid}")]
    public async Task<IActionResult> GetStatusAsync(Guid productId)
    {
        NewProductStatusDto status =
            await newProductsService.GetStatusAsync(productId);

        return Ok(status);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("status/{productId:guid}")]
    public async Task<IActionResult> UpdateStatusAsync(
        Guid productId,
        [FromBody] UpdateNewProductStatusRequest request)
    {
        try
        {
            NewProductStatusDto status =
                await newProductsService.UpdateStatusAsync(
                    productId,
                    request);

            return Ok(status);
        }
        catch (NewProductsServiceException exception)
        {
            return StatusCode(
                exception.StatusCode,
                new { message = exception.Message });
        }
    }
}

public sealed record UpdateNewProductStatusRequest(
    bool IsNewProduct,
    int DisplayDays = 14);

public sealed record NewProductStatusDto(
    bool IsNewProduct,
    int DisplayDays,
    DateTime? ActiveUntilUtc,
    bool IsCurrentlyNew);

public sealed record NewProductsPageResponse(
    IReadOnlyList<object> Items,
    int TotalCount);
