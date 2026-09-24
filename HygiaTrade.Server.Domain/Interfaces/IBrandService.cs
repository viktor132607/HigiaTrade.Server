using HygiaTrade.Common.Requests.Brand;
using HygiaTrade.Common.Responses.Brand;

namespace HygiaTrade.Domain.Interfaces;

public interface IBrandService
{
    Task<IReadOnlyList<BrandResponse>> GetAsync();
    Task<BrandResponse> CreateAsync(BrandRequest request);
    Task<BrandResponse> UpdateAsync(UpdateBrandRequest request);
    Task DeleteAsync(Guid id);
}
