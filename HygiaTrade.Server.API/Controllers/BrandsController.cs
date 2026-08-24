using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.AspNetCore.Authorization;
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
        var brands = await db.Brands
            .AsNoTracking()
            .Where(brand => !brand.IsDeleted)
            .OrderBy(brand => brand.Name)
            .Select(brand => new BrandResponse(
                brand.Id,
                brand.Name,
                brand.ThumbnailImageUrl,
                brand.Description,
                db.Products.Count(product =>
                    !product.IsDeleted &&
                    product.IsActive &&
                    product.Brand != null &&
                    product.Brand.ToLower() == brand.Name.ToLower())))
            .ToListAsync();

        return Ok(brands);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] BrandRequest request)
    {
        string name = request.Name?.Trim() ?? string.Empty;
        if (name.Length < 2 || name.Length > 80)
        {
            return BadRequest(new { message = "Brand name must be between 2 and 80 characters." });
        }

        bool exists = await db.Brands.AnyAsync(brand =>
            !brand.IsDeleted && brand.Name.ToLower() == name.ToLower());
        if (exists)
        {
            return Conflict(new { message = "A brand with this name already exists." });
        }

        Brand brand = new()
        {
            Name = name,
            ThumbnailImageUrl = NormalizeOptional(request.ThumbnailImageUrl),
            Description = NormalizeOptional(request.Description)
        };

        db.Brands.Add(brand);
        await db.SaveChangesAsync();

        return Ok(new BrandResponse(
            brand.Id,
            brand.Name,
            brand.ThumbnailImageUrl,
            brand.Description,
            0));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateBrandRequest request)
    {
        Brand? brand = await db.Brands.FirstOrDefaultAsync(item =>
            item.Id == request.Id && !item.IsDeleted);
        if (brand is null)
        {
            return NotFound(new { message = "Brand not found." });
        }

        string name = request.Name?.Trim() ?? string.Empty;
        if (name.Length < 2 || name.Length > 80)
        {
            return BadRequest(new { message = "Brand name must be between 2 and 80 characters." });
        }

        bool duplicate = await db.Brands.AnyAsync(item =>
            item.Id != brand.Id &&
            !item.IsDeleted &&
            item.Name.ToLower() == name.ToLower());
        if (duplicate)
        {
            return Conflict(new { message = "A brand with this name already exists." });
        }

        string oldName = brand.Name;
        if (!string.Equals(oldName, name, StringComparison.Ordinal))
        {
            var products = await db.Products
                .Where(product =>
                    !product.IsDeleted &&
                    product.Brand != null &&
                    product.Brand.ToLower() == oldName.ToLower())
                .ToListAsync();

            foreach (Product product in products)
            {
                product.Brand = name;
                product.ModifiedOn = DateTime.UtcNow;
            }
        }

        brand.Name = name;
        brand.ThumbnailImageUrl = NormalizeOptional(request.ThumbnailImageUrl);
        brand.Description = NormalizeOptional(request.Description);
        brand.ModifiedOn = DateTime.UtcNow;

        await db.SaveChangesAsync();

        int productCount = await db.Products.CountAsync(product =>
            !product.IsDeleted &&
            product.IsActive &&
            product.Brand != null &&
            product.Brand.ToLower() == brand.Name.ToLower());

        return Ok(new BrandResponse(
            brand.Id,
            brand.Name,
            brand.ThumbnailImageUrl,
            brand.Description,
            productCount));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        Brand? brand = await db.Brands.FirstOrDefaultAsync(item =>
            item.Id == id && !item.IsDeleted);
        if (brand is null)
        {
            return NotFound(new { message = "Brand not found." });
        }

        int assignedProducts = await db.Products.CountAsync(product =>
            !product.IsDeleted &&
            product.Brand != null &&
            product.Brand.ToLower() == brand.Name.ToLower());

        if (assignedProducts > 0)
        {
            return Conflict(new
            {
                message = $"Brand cannot be deleted while it is assigned to {assignedProducts} product(s)."
            });
        }

        brand.IsDeleted = true;
        brand.ModifiedOn = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok();
    }

    private static string? NormalizeOptional(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

public sealed record BrandRequest(
    string Name,
    string? ThumbnailImageUrl,
    string? Description);

public sealed record UpdateBrandRequest(
    Guid Id,
    string Name,
    string? ThumbnailImageUrl,
    string? Description);

public sealed record BrandResponse(
    Guid Id,
    string Name,
    string? ThumbnailImageUrl,
    string? Description,
    int ProductCount);
