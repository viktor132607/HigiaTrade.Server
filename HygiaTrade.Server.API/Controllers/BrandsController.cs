using HygiaTrade.Common.Requests.Brand;
using HygiaTrade.Common.Responses.Brand;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController(IBrandService brandService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        IReadOnlyList<BrandResponse> brands = await brandService.GetAsync();
        return Ok(brands);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] BrandRequest request)
    {
        BrandResponse brand = await brandService.CreateAsync(request);
        return Ok(brand);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateBrandRequest request)
    {
        BrandResponse brand = await brandService.UpdateAsync(request);
        return Ok(brand);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        await brandService.DeleteAsync(id);
        return Ok();
    }
}
