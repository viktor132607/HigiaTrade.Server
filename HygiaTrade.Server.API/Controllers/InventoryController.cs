using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/[controller]")]
public class InventoryController(
    IInventoryService inventoryService) : ControllerBase
{
    [HttpGet("{productId:guid}")]
    public async Task<IActionResult> GetAsync(Guid productId)
    {
        try
        {
            InventoryResponse response =
                await inventoryService.GetAsync(productId);

            return Ok(response);
        }
        catch (InventoryServiceException exception)
        {
            return StatusCode(
                exception.StatusCode,
                new { message = exception.Message });
        }
    }

    [HttpPost("{productId:guid}/add")]
    public async Task<IActionResult> AddAsync(
        Guid productId,
        [FromBody] AddStockRequest request)
    {
        try
        {
            AddStockResponse response =
                await inventoryService.AddAsync(
                    productId,
                    request);

            return Ok(response);
        }
        catch (InventoryServiceException exception)
        {
            return StatusCode(
                exception.StatusCode,
                new { message = exception.Message });
        }
    }
}

public sealed record AddStockRequest(
    int Quantity,
    string InvoiceNumber);

public sealed record StockEntryResponse(
    Guid Id,
    int Quantity,
    string InvoiceNumber,
    DateTime CreatedOn);

public sealed record InventoryResponse(
    uint CurrentQuantity,
    IReadOnlyList<StockEntryResponse> Entries);

public sealed record AddStockResponse(
    uint CurrentQuantity,
    StockEntryResponse Entry);
