using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HygiaTrade.API.Helpers;
using HygiaTrade.Common.Requests.Category;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync()
    {
        return await ControllerProcessor.ProcessAsync(() => categoryService.GetAsync(), this);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        return await ControllerProcessor.ProcessAsync(() => categoryService.GetByIdAsync(id), this);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateCategoryRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => categoryService.CreateAsync(request), this, true);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateCategoryRequest request)
    {
        return await ControllerProcessor.ProcessAsync(() => categoryService.UpdateAsync(request), this, true);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        return await ControllerProcessor.ProcessAsync<object>(
            async () => await categoryService.DeleteAsync(id), this);
    }
}
