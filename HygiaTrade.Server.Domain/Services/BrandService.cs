using HygiaTrade.Common.Requests.Brand;
using HygiaTrade.Common.Responses.Brand;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Domain.Services;

public sealed class BrandService(ApplicationDbContext db) : IBrandService
{
    public async Task<IReadOnlyList<BrandResponse>> GetAsync()
    {
        return await db.Brands
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
    }

    public async Task<BrandResponse> CreateAsync(BrandRequest request)
    {
        string name = NormalizeName(request.Name);

        bool exists = await db.Brands.AnyAsync(brand =>
            !brand.IsDeleted &&
            brand.Name.ToLower() == name.ToLower());

        if (exists)
        {
            throw new AppException("A brand with this name already exists.")
                .SetStatusCode(409);
        }

        Brand brand = new()
        {
            Name = name,
            ThumbnailImageUrl = NormalizeOptional(request.ThumbnailImageUrl),
            Description = NormalizeOptional(request.Description)
        };

        db.Brands.Add(brand);
        await db.SaveChangesAsync();

        return ToResponse(brand, 0);
    }

    public async Task<BrandResponse> UpdateAsync(UpdateBrandRequest request)
    {
        Brand? brand = await db.Brands.FirstOrDefaultAsync(item =>
            item.Id == request.Id &&
            !item.IsDeleted);

        if (brand is null)
        {
            throw new AppException("Brand not found.")
                .SetStatusCode(404);
        }

        string name = NormalizeName(request.Name);

        bool duplicate = await db.Brands.AnyAsync(item =>
            item.Id != brand.Id &&
            !item.IsDeleted &&
            item.Name.ToLower() == name.ToLower());

        if (duplicate)
        {
            throw new AppException("A brand with this name already exists.")
                .SetStatusCode(409);
        }

        string oldName = brand.Name;
        DateTime modifiedOn = DateTime.UtcNow;

        if (!string.Equals(oldName, name, StringComparison.Ordinal))
        {
            List<Product> products = await db.Products
                .Where(product =>
                    !product.IsDeleted &&
                    product.Brand != null &&
                    product.Brand.ToLower() == oldName.ToLower())
                .ToListAsync();

            foreach (Product product in products)
            {
                product.Brand = name;
                product.ModifiedOn = modifiedOn;
            }
        }

        brand.Name = name;
        brand.ThumbnailImageUrl = NormalizeOptional(request.ThumbnailImageUrl);
        brand.Description = NormalizeOptional(request.Description);
        brand.ModifiedOn = modifiedOn;

        await db.SaveChangesAsync();

        int productCount = await db.Products.CountAsync(product =>
            !product.IsDeleted &&
            product.IsActive &&
            product.Brand != null &&
            product.Brand.ToLower() == brand.Name.ToLower());

        return ToResponse(brand, productCount);
    }

    public async Task DeleteAsync(Guid id)
    {
        Brand? brand = await db.Brands.FirstOrDefaultAsync(item =>
            item.Id == id &&
            !item.IsDeleted);

        if (brand is null)
        {
            throw new AppException("Brand not found.")
                .SetStatusCode(404);
        }

        int assignedProducts = await db.Products.CountAsync(product =>
            !product.IsDeleted &&
            product.Brand != null &&
            product.Brand.ToLower() == brand.Name.ToLower());

        if (assignedProducts > 0)
        {
            throw new AppException(
                    $"Brand cannot be deleted while it is assigned to {assignedProducts} product(s).")
                .SetStatusCode(409);
        }

        brand.IsDeleted = true;
        brand.ModifiedOn = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static string NormalizeName(string? value)
    {
        string name = value?.Trim() ?? string.Empty;

        if (name.Length < 2 || name.Length > 80)
        {
            throw new AppException("Brand name must be between 2 and 80 characters.")
                .SetStatusCode(400);
        }

        return name;
    }

    private static string? NormalizeOptional(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized;
    }

    private static BrandResponse ToResponse(Brand brand, int productCount)
    {
        return new BrandResponse(
            brand.Id,
            brand.Name,
            brand.ThumbnailImageUrl,
            brand.Description,
            productCount);
    }
}
