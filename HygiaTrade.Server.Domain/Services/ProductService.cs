using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Core.Pages;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public sealed class ProductService(
    IProductReadService readService,
    IProductMutationService mutationService,
    IProductSearchService searchService) : IProductService
{
    public Task<IEnumerable<ProductResponse>?> GetAsync() =>
        readService.GetAsync();

    public Task<IEnumerable<ProductResponse>?> GetBestSellersAsync(
        int numOfBestSellers) =>
        readService.GetBestSellersAsync(numOfBestSellers);

    public Task<ProductResponse?> GetByIdAsync(Guid id) =>
        readService.GetByIdAsync(id);

    public Task<ProductPriceQuoteResponse> GetPriceQuoteAsync(
        Guid id,
        int quantity) =>
        readService.GetPriceQuoteAsync(id, quantity);

    public Task<ProductResponse?> CreateAsync(
        CreateProductRequest request) =>
        mutationService.CreateAsync(request);

    public Task<ProductResponse?> UpdateAsync(
        UpdateProductRequest request) =>
        mutationService.UpdateAsync(request);

    public Task<bool> DeleteAsync(Guid id) =>
        mutationService.DeleteAsync(id);

    public Task<Paginated<ProductsResponse>> SearchProductsAsync(
        SearchProductsRequest request) =>
        searchService.SearchProductsAsync(request);
}
