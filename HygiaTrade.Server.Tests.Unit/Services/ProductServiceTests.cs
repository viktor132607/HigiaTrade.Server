using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Core.Pages;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class ProductServiceTests
{
    private readonly Mock<IProductReadService> read = new();
    private readonly Mock<IProductMutationService> mutation = new();
    private readonly Mock<IProductSearchService> search = new();

    private ProductService CreateService() =>
        new(read.Object, mutation.Object, search.Object);

    [Fact]
    public async Task GetAsync_Delegates()
    {
        IEnumerable<ProductResponse> expected = [Response()];
        read.Setup(x => x.GetAsync()).ReturnsAsync(expected);
        Assert.Same(expected, await CreateService().GetAsync());
    }

    [Fact]
    public async Task GetBestSellersAsync_Delegates()
    {
        IEnumerable<ProductResponse> expected = [Response()];
        read.Setup(x => x.GetBestSellersAsync(2)).ReturnsAsync(expected);
        Assert.Same(expected, await CreateService().GetBestSellersAsync(2));
    }

    [Fact]
    public async Task GetByIdAsync_Delegates()
    {
        Guid id = Guid.NewGuid();
        ProductResponse expected = Response();
        read.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(expected);
        Assert.Same(expected, await CreateService().GetByIdAsync(id));
    }

    [Fact]
    public async Task GetPriceQuoteAsync_Delegates()
    {
        Guid id = Guid.NewGuid();
        var expected = new ProductPriceQuoteResponse { ProductId = id, Quantity = 3 };
        read.Setup(x => x.GetPriceQuoteAsync(id, 3)).ReturnsAsync(expected);
        Assert.Same(expected, await CreateService().GetPriceQuoteAsync(id, 3));
    }

    [Fact]
    public async Task CreateAsync_Delegates()
    {
        var request = CreateRequest();
        ProductResponse expected = Response();
        mutation.Setup(x => x.CreateAsync(request)).ReturnsAsync(expected);
        Assert.Same(expected, await CreateService().CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_Delegates()
    {
        var request = UpdateRequest();
        ProductResponse expected = Response();
        mutation.Setup(x => x.UpdateAsync(request)).ReturnsAsync(expected);
        Assert.Same(expected, await CreateService().UpdateAsync(request));
    }

    [Fact]
    public async Task DeleteAsync_Delegates()
    {
        Guid id = Guid.NewGuid();
        mutation.Setup(x => x.DeleteAsync(id)).ReturnsAsync(true);
        Assert.True(await CreateService().DeleteAsync(id));
    }

    [Fact]
    public async Task SearchProductsAsync_Delegates()
    {
        var request = new SearchProductsRequest();
        var expected = new Paginated<ProductsResponse> { Items = [], TotalCount = 0 };
        search.Setup(x => x.SearchProductsAsync(request)).ReturnsAsync(expected);
        Assert.Same(expected, await CreateService().SearchProductsAsync(request));
    }

    private static ProductResponse Response() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "P",
            Description = "D",
            MainImageUrl = "img",
            CategoryName = ""
        };

    private static CreateProductRequest CreateRequest() =>
        new()
        {
            Title = "P",
            Description = "D",
            MainImageUrl = "img",
            CategoryId = Guid.NewGuid()
        };

    private static UpdateProductRequest UpdateRequest() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "P",
            Description = "D",
            MainImageUrl = "img",
            CategoryId = Guid.NewGuid()
        };
}
