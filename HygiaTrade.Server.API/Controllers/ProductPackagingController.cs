using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.API.Controllers;

public sealed record PackagingInput(Guid? Id, string Label, int UnitsPerPackage,
    decimal PriceInclVat, uint StockQuantity, bool IsDefault);

[ApiController]
[Route("api/Products/{productId:guid}/packaging")]
public sealed class ProductPackagingController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid productId)
    {
        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == productId && !p.IsDeleted && (p.IsActive || User.IsInRole(Roles.Admin)))
            .Select(p => new { p.Id, p.RegularPrice, p.Quantity })
            .FirstOrDefaultAsync();
        if (product is null) return NotFound();

        var options = await db.ProductPackagingOptions.AsNoTracking()
            .Where(o => o.ProductId == productId && o.IsActive && !o.IsDeleted)
            .OrderBy(o => o.SortOrder)
            .ToListAsync();

        return Ok(options.Select(o => new
        {
            o.Id, o.Label, o.UnitsPerPackage, o.IsDefault, o.SortOrder,
            PriceInclVat = o.IsDefault ? product.RegularPrice : o.PriceInclVat,
            StockQuantity = o.IsDefault ? product.Quantity : o.StockQuantity
        }));
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut]
    public async Task<IActionResult> Put(Guid productId, [FromBody] List<PackagingInput> requested)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
        if (product is null) return NotFound();
        if (requested is null || requested.Count is < 1 or > 20 ||
            requested.Count(x => x.IsDefault) != 1 ||
            requested.Any(x => string.IsNullOrWhiteSpace(x.Label) || x.Label.Trim().Length > 100 ||
                               x.UnitsPerPackage is < 1 or > 10000 ||
                               x.PriceInclVat is < 0m or > 1000000m || (!x.IsDefault && x.StockQuantity > 0 && x.PriceInclVat == 0m)) ||
            requested.Select(x => x.Label.Trim().ToUpperInvariant()).Distinct().Count() != requested.Count ||
            requested.Where(x => x.Id.HasValue && x.Id.Value != Guid.Empty).Select(x => x.Id).Distinct().Count()
                != requested.Count(x => x.Id.HasValue && x.Id.Value != Guid.Empty))
            return BadRequest(new { message = "Invalid packaging options or duplicate labels." });

        var stored = await db.ProductPackagingOptions
            .Where(o => o.ProductId == productId && !o.IsDeleted).ToListAsync();

        if (stored.Any(x => x.IsDefault && !requested.Any(y => y.Id == x.Id && y.IsDefault)))
            return BadRequest(new { message = "The default package cannot be replaced." });

        for (int index = 0; index < requested.Count; index++)
        {
            var row = requested[index];
            var existing = row.Id is Guid id && id != Guid.Empty
                ? stored.FirstOrDefault(x => x.Id == id) : null;
            if (row.Id is Guid supplied && supplied != Guid.Empty && existing is null)
                return BadRequest(new { message = "Unknown packaging option." });

            var option = existing ?? new ProductPackagingOption { ProductId = productId };
            option.Label = row.Label.Trim();
            option.UnitsPerPackage = row.UnitsPerPackage;
            option.IsDefault = row.IsDefault;
            option.PriceInclVat = row.IsDefault ? product.RegularPrice : decimal.Round(row.PriceInclVat, 2);
            option.StockQuantity = row.IsDefault ? product.Quantity : row.StockQuantity;
            option.SortOrder = index;
            option.IsActive = true;
            option.ModifiedOn = DateTime.UtcNow;
            if (existing is null) db.ProductPackagingOptions.Add(option);
        }

        var retained = requested.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
        foreach (var old in stored.Where(x => !retained.Contains(x.Id)))
        {
            old.IsActive = false;
            old.ModifiedOn = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return await Get(productId);
    }
}
