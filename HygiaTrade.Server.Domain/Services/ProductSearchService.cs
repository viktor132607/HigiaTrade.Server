using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Core.Pages;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Data.PaginationAndFiltering;

namespace HygiaTrade.Domain.Services;

public interface IProductSearchService
{
    Task<Paginated<ProductsResponse>> SearchProductsAsync(
        SearchProductsRequest request);
}

public sealed class ProductSearchService(
    IProductRepository productRepository)
    : IProductSearchService
{
    public async Task<Paginated<ProductsResponse>>
        SearchProductsAsync(SearchProductsRequest request)
    {
        request ??= new SearchProductsRequest();

        var filter = new Filter<Product>
        {
            Includes =
            [
                product => product.Category!
            ],
            Predicate = request.GetPredicate(),
            PageNumber = request.PageNumber ?? 1,
            PageSize = request.PageSize ?? 10,
            SortBy = request.SortBy ?? "RegularPrice",
            SortDescending =
                request.SortDescending ?? false,
        };

        Paginated<Product> result =
            await productRepository.SearchAsync(filter);

        return new Paginated<ProductsResponse>
        {
            Items = result.Items?
                .Select(
                    ProductMapping.ToProductsResponse)
                .ToList()
                ?? [],
            TotalCount = result.TotalCount
        };
    }
}
