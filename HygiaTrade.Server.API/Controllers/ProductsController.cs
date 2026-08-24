using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HygiaTrade.API.Helpers;
using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync([FromQuery] SearchProductsRequest? request)
    {
        SearchProductsRequest searchRequest = request ?? new SearchProductsRequest();
        searchRequest.IncludeInactive = User.IsInRole(Roles.Admin);

        return await ControllerProcessor.ProcessAsync(
            () => productService.SearchProductsAsync(searchRequest),
            this,
            true);
    }
    
    [HttpGet("best-sellers")]
    public async Task<IActionResult> GetBestSellersAsync(int numOfBestSellers)
    {
        return await ControllerProcessor.ProcessAsync(
            () => productService.GetBestSellersAsync(numOfBestSellers),
            this,
            true);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        return await ControllerProcessor.ProcessAsync(() => productService.GetByIdAsync(id), this);
    }

    [HttpGet("{id}/price")]
    public async Task<IActionResult> GetPriceQuoteAsync(Guid id, [FromQuery] int quantity = 1)
    {
        return await ControllerProcessor.ProcessAsync(
            () => productService.GetPriceQuoteAsync(id, quantity),
            this);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateProductRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => productService.CreateAsync(request), this, true);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateProductRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => productService.UpdateAsync(request), this, true);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        return await ControllerProcessor.ProcessAsync<object>(
            async () => await productService.DeleteAsync(id), this);
    }
}
