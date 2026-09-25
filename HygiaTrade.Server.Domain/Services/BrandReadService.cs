using HygiaTrade.Common.Responses.Brand;

namespace HygiaTrade.Domain.Services;

public interface IBrandReadService
{
    Task<IReadOnlyList<BrandResponse>> GetAsync();
}

public sealed class BrandReadService(
    IBrandRepository repository,
    IBrandMapper mapper)
    : IBrandReadService
{
    public async Task<IReadOnlyList<BrandResponse>> GetAsync()
    {
        IReadOnlyList<BrandListItem> brands =
            await repository.GetAllAsync();

        return brands
            .Select(mapper.ToResponse)
            .ToList();
    }
}
