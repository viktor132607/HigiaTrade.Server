using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Core.Pages;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Data.PaginationAndFiltering;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class ProductSearchServiceTests
{
    private readonly Mock<IProductRepository> products = new();

    private ProductSearchService CreateService() => new(products.Object);

    [Fact]
    public async Task SearchProductsAsync_UsesDefaultsAndMapsResults()
    {
        Filter<Product>? captured = null;
        products.Setup(x => x.SearchAsync(It.IsAny<Filter<Product>>()))
            .Callback<Filter<Product>>(filter => captured = filter)
            .ReturnsAsync(new Paginated<Product>
            {
                Items = [Product("A")],
                TotalCount = 1
            });

        var result = await CreateService().SearchProductsAsync(new SearchProductsRequest());

        Assert.Single(result.Items!);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, captured!.PageNumber);
        Assert.Equal(10, captured.PageSize);
        Assert.Equal("RegularPrice", captured.SortBy);
        Assert.False(captured.SortDescending);
    }

    [Fact]
    public async Task SearchProductsAsync_PreservesExplicitPagingAndSort()
    {
        Filter<Product>? captured = null;
        products.Setup(x => x.SearchAsync(It.IsAny<Filter<Product>>()))
            .Callback<Filter<Product>>(filter => captured = filter)
            .ReturnsAsync(new Paginated<Product> { Items = [], TotalCount = 0 });

        await CreateService().SearchProductsAsync(
            new SearchProductsRequest
            {
                PageNumber = 3,
                PageSize = 20,
                SortBy = "Title",
                SortDescending = true
            });

        Assert.Equal(3, captured!.PageNumber);
        Assert.Equal(20, captured.PageSize);
        Assert.Equal("Title", captured.SortBy);
        Assert.True(captured.SortDescending);
    }

    [Fact]
    public async Task SearchProductsAsync_ReturnsEmptyList_WhenRepositoryItemsNull()
    {
        products.Setup(x => x.SearchAsync(It.IsAny<Filter<Product>>()))
            .ReturnsAsync(new Paginated<Product> { Items = null, TotalCount = 0 });

        var result = await CreateService().SearchProductsAsync(new SearchProductsRequest());

        Assert.NotNull(result.Items);
        Assert.Empty(result.Items!);
    }

    private static Product Product(string title) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "D",
            MainImageUrl = "img"
        };
}
