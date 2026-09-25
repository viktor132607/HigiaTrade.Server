using HygiaTrade.Common.Requests.Brand;
using HygiaTrade.Common.Responses.Brand;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public sealed class BrandService(
    IBrandReadService readService,
    IBrandMutationService mutationService)
    : IBrandService
{
    public Task<IReadOnlyList<BrandResponse>> GetAsync() =>
        readService.GetAsync();

    public Task<BrandResponse> CreateAsync(
        BrandRequest request) =>
        mutationService.CreateAsync(request);

    public Task<BrandResponse> UpdateAsync(
        UpdateBrandRequest request) =>
        mutationService.UpdateAsync(request);

    public Task DeleteAsync(Guid id) =>
        mutationService.DeleteAsync(id);
}
