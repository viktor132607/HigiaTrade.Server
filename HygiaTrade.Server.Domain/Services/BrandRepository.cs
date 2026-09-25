using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HygiaTrade.Domain.Services;

public sealed record BrandListItem(
    Guid Id,
    string Name,
    string? ThumbnailImageUrl,
    string? Description,
    int ProductCount);

public interface IBrandRepository
{
    Task<IReadOnlyList<BrandListItem>> GetAllAsync();

    Task<Brand?> GetTrackedAsync(Guid id);

    Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludedId = null);

    Task<int> GetActiveProductCountAsync(
        string brandName);

    Task<int> GetAssignedProductCountAsync(
        string brandName);

    Task RenameProductsAsync(
        string oldName,
        string newName,
        DateTime modifiedOn);

    void Add(Brand brand);

    Task SaveChangesAsync();
}

public sealed class BrandRepository(
    ApplicationDbContext db)
    : IBrandRepository
{
    public async Task<IReadOnlyList<BrandListItem>>
        GetAllAsync() =>
        await db.Brands
            .AsNoTracking()
            .Where(brand => !brand.IsDeleted)
            .OrderBy(brand => brand.Name)
            .Select(brand => new BrandListItem(
                brand.Id,
                brand.Name,
                brand.ThumbnailImageUrl,
                brand.Description,
                db.Products.Count(product =>
                    !product.IsDeleted &&
                    product.IsActive &&
                    product.Brand != null &&
                    product.Brand.ToLower() ==
                        brand.Name.ToLower())))
            .ToListAsync();

    public Task<Brand?> GetTrackedAsync(
        Guid id) =>
        db.Brands.FirstOrDefaultAsync(
            brand =>
                brand.Id == id &&
                !brand.IsDeleted);

    public Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludedId = null) =>
        db.Brands.AnyAsync(brand =>
            !brand.IsDeleted &&
            (!excludedId.HasValue ||
                brand.Id != excludedId.Value) &&
            brand.Name.ToLower() ==
                name.ToLower());

    public Task<int> GetActiveProductCountAsync(
        string brandName) =>
        db.Products.CountAsync(product =>
            !product.IsDeleted &&
            product.IsActive &&
            product.Brand != null &&
            product.Brand.ToLower() ==
                brandName.ToLower());

    public Task<int> GetAssignedProductCountAsync(
        string brandName) =>
        db.Products.CountAsync(product =>
            !product.IsDeleted &&
            product.Brand != null &&
            product.Brand.ToLower() ==
                brandName.ToLower());

    public async Task RenameProductsAsync(
        string oldName,
        string newName,
        DateTime modifiedOn)
    {
        List<Product> products =
            await db.Products
                .Where(product =>
                    !product.IsDeleted &&
                    product.Brand != null &&
                    product.Brand.ToLower() ==
                        oldName.ToLower())
                .ToListAsync();

        foreach (Product product in products)
        {
            product.Brand = newName;
            product.ModifiedOn = modifiedOn;
        }
    }

    public void Add(Brand brand) =>
        db.Brands.Add(brand);

    public async Task SaveChangesAsync() =>
        await db.SaveChangesAsync();
}
