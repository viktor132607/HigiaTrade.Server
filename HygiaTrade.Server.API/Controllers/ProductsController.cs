using HygiaTrade.API.Helpers;
using HygiaTrade.API.Services;
using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Core.Pages;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(
    IProductService productService,
    IProductImagePresentationService imagePresentationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] SearchProductsRequest? request)
    {
        SearchProductsRequest searchRequest =
            request ?? new SearchProductsRequest();

        searchRequest.IncludeInactive =
            User.IsInRole(Roles.Admin);

        return await ControllerProcessor.ProcessAsync(
            async () =>
            {
                Paginated<ProductsResponse> result =
                    await productService
                        .SearchProductsAsync(searchRequest);

                await imagePresentationService.ResolveAsync(
                    result.Items ??
                    Enumerable.Empty<ProductsResponse>());

                return result;
            },
            this,
            true);
    }

    [HttpGet("best-sellers")]
    public async Task<IActionResult> GetBestSellersAsync(
        int numOfBestSellers)
    {
        return await ControllerProcessor.ProcessAsync(
            async () =>
            {
                IEnumerable<ProductResponse>? products =
                    await productService
                        .GetBestSellersAsync(
                            numOfBestSellers);

                List<ProductResponse> resolved =
                    products?.ToList() ?? [];

                await imagePresentationService.ResolveAsync(
                    resolved);

                return resolved;
            },
            this,
            true);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        return await ControllerProcessor.ProcessAsync(
            async () =>
            {
                ProductResponse? product =
                    await productService.GetByIdAsync(id);

                if (product is not null)
                {
                    await imagePresentationService.ResolveAsync(
                        [product]);
                }

                return product;
            },
            this);
    }

    [HttpGet("{id}/price")]
    public async Task<IActionResult> GetPriceQuoteAsync(
        Guid id,
        [FromQuery] int quantity = 1)
    {
        return await ControllerProcessor.ProcessAsync(
            () => productService
                .GetPriceQuoteAsync(id, quantity),
            this);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateProductRequest request)
    {
        return await ControllerProcessor.ProcessAsync(
            async () =>
            {
                await imagePresentationService
                    .PrepareForPersistenceAsync(request);

                ProductResponse? product =
                    await productService.CreateAsync(request);

                if (product is not null)
                {
                    await imagePresentationService.ResolveAsync(
                        [product]);
                }

                return product;
            },
            this,
            true);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut]
    public async Task<IActionResult> UpdateAsync(
        [FromBody] UpdateProductRequest request)
    {
        return await ControllerProcessor.ProcessAsync(
            async () =>
            {
                await imagePresentationService
                    .PrepareForPersistenceAsync(request);

                ProductResponse? product =
                    await productService.UpdateAsync(request);

                if (product is not null)
                {
                    await imagePresentationService.ResolveAsync(
                        [product]);
                }

                return product;
            },
            this,
            true);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        return await ControllerProcessor.ProcessAsync<object>(
            async () => await productService.DeleteAsync(id),
            this);
    }
}
