using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Core.Pages;

namespace HygiaTrade.Domain.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductResponse>?> GetAsync();
    Task<IEnumerable<ProductResponse>?> GetBestSellersAsync(int numOfBestSellers);
    Task<ProductResponse?> GetByIdAsync(Guid id);
    Task<ProductResponse?> UpdateAsync(UpdateProductRequest request);
    Task<ProductResponse?> CreateAsync(CreateProductRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task<Paginated<ProductsResponse>> SearchProductsAsync(SearchProductsRequest request);

}
