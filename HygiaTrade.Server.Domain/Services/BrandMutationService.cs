using HygiaTrade.Common.Requests.Brand;
using HygiaTrade.Common.Responses.Brand;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Domain.Services;

public interface IBrandMutationService
{
    Task<BrandResponse> CreateAsync(
        BrandRequest request);

    Task<BrandResponse> UpdateAsync(
        UpdateBrandRequest request);

    Task DeleteAsync(Guid id);
}

public sealed class BrandMutationService(
    IBrandRepository repository,
    IBrandPolicy policy,
    IBrandMapper mapper,
    IBrandClock clock)
    : IBrandMutationService
{
    public async Task<BrandResponse> CreateAsync(
        BrandRequest request)
    {
        string name =
            policy.NormalizeName(request.Name);

        if (await repository.ExistsByNameAsync(
                name))
        {
            policy.ThrowDuplicate();
        }

        Brand brand = new()
        {
            Name = name,
            ThumbnailImageUrl =
                policy.NormalizeOptional(
                    request.ThumbnailImageUrl),
            Description =
                policy.NormalizeOptional(
                    request.Description)
        };

        repository.Add(brand);
        await repository.SaveChangesAsync();

        return mapper.ToResponse(
            brand,
            0);
    }

    public async Task<BrandResponse> UpdateAsync(
        UpdateBrandRequest request)
    {
        Brand brand =
            await repository.GetTrackedAsync(
                request.Id)
            ?? policy.ThrowNotFound<Brand>();

        string name =
            policy.NormalizeName(request.Name);

        if (await repository.ExistsByNameAsync(
                name,
                brand.Id))
        {
            policy.ThrowDuplicate();
        }

        string oldName = brand.Name;
        DateTime modifiedOn = clock.UtcNow;

        if (!string.Equals(
                oldName,
                name,
                StringComparison.Ordinal))
        {
            await repository.RenameProductsAsync(
                oldName,
                name,
                modifiedOn);
        }

        brand.Name = name;
        brand.ThumbnailImageUrl =
            policy.NormalizeOptional(
                request.ThumbnailImageUrl);
        brand.Description =
            policy.NormalizeOptional(
                request.Description);
        brand.ModifiedOn = modifiedOn;

        await repository.SaveChangesAsync();

        int productCount =
            await repository.GetActiveProductCountAsync(
                brand.Name);

        return mapper.ToResponse(
            brand,
            productCount);
    }

    public async Task DeleteAsync(Guid id)
    {
        Brand brand =
            await repository.GetTrackedAsync(id)
            ?? policy.ThrowNotFound<Brand>();

        int assignedProducts =
            await repository.GetAssignedProductCountAsync(
                brand.Name);

        policy.EnsureCanDelete(
            assignedProducts);

        brand.IsDeleted = true;
        brand.ModifiedOn = clock.UtcNow;

        await repository.SaveChangesAsync();
    }
}
