using HygiaTrade.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        var brands = await db.Products
            .AsNoTracking()
            .Where(product =>
                !product.IsDeleted &&
                product.IsActive &&
                product.Brand != null &&
                product.Brand != "")
            .GroupBy(product => product.Brand!)
            .Select(group => new
            {
                name = group.Key,
                productCount = group.Count()
            })
            .OrderBy(brand => brand.name)
            .ToListAsync();

        return Ok(brands);
    }
}
