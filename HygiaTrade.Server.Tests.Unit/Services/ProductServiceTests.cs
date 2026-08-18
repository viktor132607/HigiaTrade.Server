using Moq;
using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HygiaTrade.Common.Requests.Image;
using HygiaTrade.Core.Pages;
using HygiaTrade.Data.PaginationAndFiltering;
using Xunit;

namespace HygiaTrade.Tests.Unit.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> productRepositoryMock;
        private readonly Mock<ICategoryRepository> categoryRepositoryMock;
        private readonly Mock<IImageRepository> imageRepositoryMock;
        private readonly ProductService productService;

        public ProductServiceTests()
        {
            productRepositoryMock = new();
            categoryRepositoryMock = new();
            imageRepositoryMock = new();
            productService = new(
                productRepositoryMock.Object,
                categoryRepositoryMock.Object,
                imageRepositoryMock.Object);
        }

        [Fact]
        public async Task GetAsync_ShouldReturnProducts()
        {
            List<Product> products = new()
            {
                new() { Id = Guid.NewGuid(), Title = "Product1" },
                new() { Id = Guid.NewGuid(), Title = "Product2" }
            };

            productRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(products);

            IEnumerable<ProductResponse> result = await productService.GetAsync();

            Assert.NotNull(result);
            Assert.Collection(result,
                item => Assert.Equal("Product1", item.Title),
                item => Assert.Equal("Product2", item.Title));
        }

        [Fact]
        public async Task GetBestSellersAsync_ShouldReturnBestSellers()
        {
            List<Product> bestSellers = new()
            {
                new() { Id = Guid.NewGuid(), Title = "BestSeller1" },
                new() { Id = Guid.NewGuid(), Title = "BestSeller2" }
            };

            productRepositoryMock.Setup(r => r.GetBestSellersAsync(2)).ReturnsAsync(bestSellers);

            IEnumerable<ProductResponse> result = await productService.GetBestSellersAsync(2);

            Assert.NotNull(result);
            Assert.Collection(result,
                item => Assert.Equal("BestSeller1", item.Title),
                item => Assert.Equal("BestSeller2", item.Title));
        }

        [Fact]
        public async Task GetByIdAsync_ExistingProduct_ShouldReturnProduct()
        {
            Product product = new() { Id = Guid.NewGuid(), Title = "Product1" };

            productRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);

            ProductResponse result = await productService.GetByIdAsync(product.Id);

            Assert.NotNull(result);
            Assert.Equal("Product1", result.Title);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingProduct_ShouldThrowNotFound()
        {
            productRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product)null);

            await Assert.ThrowsAsync<AppException>(() => productService.GetByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateAsync_InvalidCategory_ShouldThrowException()
        {
            CreateProductRequest request = new()
            {
                CategoryId = Guid.NewGuid(),
                Title = null,
                Description = null,
                MainImageUrl = null
            };

            categoryRepositoryMock.Setup(r => r.GetByIdAsync(request.CategoryId)).ReturnsAsync((Category)null);

            await Assert.ThrowsAsync<AppException>(() => productService.CreateAsync(request));
        }
        
        [Fact]
        public async Task UpdateAsync_ProductNotFound_ShouldThrowNotFound()
        {
            UpdateProductRequest request = new()
            {
                Id = Guid.NewGuid(),
                Title = null,
                Description = null,
                MainImageUrl = null
            };

            productRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product)null);

            await Assert.ThrowsAsync<AppException>(() => productService.UpdateAsync(request));
        }

        [Fact]
        public async Task DeleteAsync_ExistingProduct_ShouldDeleteProduct()
        {
            Guid productId = Guid.NewGuid();
            productRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(new Product { Id = productId });
            imageRepositoryMock.Setup(r => r.DeleteAsync(It.IsAny<Guid>())).ReturnsAsync(true);
            productRepositoryMock.Setup(r => r.DeleteAsync(productId)).ReturnsAsync(true);

            bool result = await productService.DeleteAsync(productId);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingProduct_ShouldThrowNotFound()
        {
            productRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Product)null);

            await Assert.ThrowsAsync<AppException>(() => productService.DeleteAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task SearchProductsAsync_ValidRequest_ShouldReturnPaginatedProducts()
        {
            SearchProductsRequest request = new() { PageNumber = 1, PageSize = 10 };

            productRepositoryMock.Setup(r => r.SearchAsync(It.IsAny<Filter<Product>>())).ReturnsAsync(new Paginated<Product>
            {
                Items = new List<Product> { new() { Id = Guid.NewGuid(), Title = "Product" } },
                TotalCount = 1
            });

            Paginated<ProductsResponse> result = await productService.SearchProductsAsync(request);

            Assert.NotNull(result);
            Assert.Single(result.Items);
        }
    }
}
