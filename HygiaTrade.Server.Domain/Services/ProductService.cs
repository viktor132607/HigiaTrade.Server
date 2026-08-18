using HygiaTrade.Common.Requests.Image;
using HygiaTrade.Common.Requests.Product;
using HygiaTrade.Common.Responses.Image;
using HygiaTrade.Common.Responses.Product;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Core.Pages;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Data.PaginationAndFiltering;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public class ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository, IImageRepository imageRepository) : IProductService
{
    public async Task<IEnumerable<ProductResponse>?> GetAsync()
    {
        IEnumerable<Product> products = await productRepository.GetAllAsync();

        return products.Select(product => new ProductResponse()
        {
            Id = product.Id,
            Title = product.Title,
            Description = product.Description,
            MainImageUrl = product.MainImageUrl,
            RegularPrice = product.RegularPrice,
            DiscountPercentage = product.DiscountPercentage,
            DiscountedPrice = product.DiscountedPrice,
            Rating = product.Rating,
            Quantity = product.Quantity,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            SecondaryImages = product.SecondaryImages
                .Select(img => new ImageResponse
                {
                    Id = img.Id,
                    Uri = img.Uri
                })
                .ToList(),        
        });
    }

    public async Task<IEnumerable<ProductResponse>?> GetBestSellersAsync(int numOfBestSellers)
    {
        IEnumerable<Product> products = await productRepository.GetBestSellersAsync(numOfBestSellers);

        return products.Select(product => new ProductResponse()
        {
            Id = product.Id,
            Title = product.Title,
            Description = product.Description,
            MainImageUrl = product.MainImageUrl,
            RegularPrice = product.RegularPrice,
            DiscountPercentage = product.DiscountPercentage,
            DiscountedPrice = product.DiscountedPrice,
            Rating = product.Rating,
            Quantity = product.Quantity,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            SecondaryImages = product.SecondaryImages
                .Select(img => new ImageResponse
                {
                    Id = img.Id,
                    Uri = img.Uri
                })
                .ToList(), 
        });    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id)
    {
        Product? product = await productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new AppException("Product not found.").SetStatusCode(404);
        }

        return new()
        {
            Id = product.Id,
            Title = product.Title,
            Description = product.Description,
            MainImageUrl = product.MainImageUrl,
            RegularPrice = product.RegularPrice,
            DiscountPercentage = product.DiscountPercentage,
            DiscountedPrice = product.DiscountedPrice,
            Rating = product.Rating,
            Quantity = product.Quantity,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? string.Empty,
            SecondaryImages = product.SecondaryImages
                .Select(img => new ImageResponse
                {
                    Id = img.Id,
                    Uri = img.Uri
                })
                .ToList(), 
        };
    }

    public async Task<ProductResponse?> CreateAsync(CreateProductRequest request)
    {
        Category? category = await categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null)
        {
            throw new AppException("Invalid category.").SetStatusCode(400);
        }
        
        
        Product product = new()
        {
            Title = request.Title,
            Description = request.Description,
            MainImageUrl = request.MainImageUrl,
            Rating = 3,            
            Quantity = request.Quantity,
            CategoryId = request.CategoryId,
        };

        if (product.RegularPrice != request.RegularPrice)
        {
            product.RegularPrice = request.RegularPrice;
        }

        if (product.DiscountPercentage != request.DiscountPercentage)
        {
            product.DiscountPercentage = request.DiscountPercentage;
            product.DiscountedPrice = product.RegularPrice * (1 - product.DiscountPercentage / 100m);
        }

        if (product.DiscountedPrice != request.DiscountedPrice && request.DiscountedPrice != 0)
        {
            product.DiscountedPrice = request.DiscountedPrice;
            product.DiscountPercentage = (byte)((1 - product.DiscountedPrice / product.RegularPrice) * 100m);
        }
        
        product = await productRepository.AddAsync(product)
            ?? throw new InvalidOperationException("Failed to persist product.");

        List<Image> images = new();

        foreach (CreateImageRequest imageRequest in request.SecondaryImages)
        {
            Image image = new()
            {
                Uri = imageRequest.Uri,
                ProductId = product.Id
            };
            images.Add(image);
            await imageRepository.AddAsync(image);
        }
        
        
        return new()
        {
            Id = product.Id,
            Title = product.Title,
            Description = product.Description,
            MainImageUrl = product.MainImageUrl,
            RegularPrice = product.RegularPrice,
            DiscountPercentage = product.DiscountPercentage,
            DiscountedPrice = product.DiscountedPrice,
            Rating = product.Rating,
            Quantity = product.Quantity,
            CategoryId = product.CategoryId,
            CategoryName = category.Name,
            SecondaryImages = images
                .Select(img => new ImageResponse
                {
                    Id = img.Id,
                    Uri = img.Uri
                })
                .ToList(), 
        };
    }

    public async Task<ProductResponse?> UpdateAsync(UpdateProductRequest request)
{
    Product? existingProduct = await productRepository.GetByIdAsync(request.Id);
    if (existingProduct == null)
    {
        throw new AppException("Product not found.").SetStatusCode(404);
    }

    Category? category = await categoryRepository.GetByIdAsync(request.CategoryId);
    if (category == null)
    {
        throw new AppException("Invalid category.").SetStatusCode(400);
    }

    existingProduct.Title = request.Title;
    existingProduct.Description = request.Description;
    existingProduct.MainImageUrl = request.MainImageUrl;
    existingProduct.Quantity = request.Quantity;
    existingProduct.CategoryId = request.CategoryId;

    if (existingProduct.RegularPrice != request.RegularPrice)
    {
        existingProduct.RegularPrice = request.RegularPrice;
    }

    if (existingProduct.DiscountPercentage != request.DiscountPercentage)
    {
        existingProduct.DiscountPercentage = request.DiscountPercentage;
        existingProduct.DiscountedPrice = existingProduct.RegularPrice * (1 - existingProduct.DiscountPercentage / 100m);
    }

    if (existingProduct.DiscountedPrice != request.DiscountedPrice && request.DiscountedPrice != 0)
    {
        existingProduct.DiscountedPrice = request.DiscountedPrice;
        existingProduct.DiscountPercentage = (byte)((1 - existingProduct.DiscountedPrice / existingProduct.RegularPrice) * 100m);
    }

    foreach (Image image in existingProduct.SecondaryImages.ToList())
    {
        await imageRepository.DeleteAsync(image.Id);
    }

    existingProduct.SecondaryImages.Clear();

    foreach (UpdateImageRequest imageRequest in request.SecondaryImages)
    {
        Image newImage = new()
        {
            Uri = imageRequest.Uri,
            ProductId = existingProduct.Id
        };

        existingProduct.SecondaryImages.Add(newImage);
        await imageRepository.AddAsync(newImage);
    }

    Product updatedProduct = await productRepository.UpdateAsync(existingProduct)
        ?? throw new InvalidOperationException("Failed to persist product updates.");

    return new()
    {
        Id = updatedProduct.Id,
        Title = updatedProduct.Title,
        Description = updatedProduct.Description,
        MainImageUrl = updatedProduct.MainImageUrl,
        RegularPrice = updatedProduct.RegularPrice,
        DiscountPercentage = updatedProduct.DiscountPercentage,
        DiscountedPrice = updatedProduct.DiscountedPrice,
        Rating = updatedProduct.Rating,
        Quantity = updatedProduct.Quantity,
        CategoryId = updatedProduct.CategoryId,
        CategoryName = category.Name,
        SecondaryImages = updatedProduct.SecondaryImages
            .Select(img => new ImageResponse
            {
                Id = img.Id,
                Uri = img.Uri
            })
            .ToList()
    };
}

    public async Task<bool> DeleteAsync(Guid id)
    {
        Product? product = await productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new AppException("Product not found.").SetStatusCode(404);
        }

        foreach (Image image in product.SecondaryImages.ToList())
        {
            await imageRepository.DeleteAsync(image.Id);
        }

        return await productRepository.DeleteAsync(id);
    }

    public async Task<Paginated<ProductsResponse>> SearchProductsAsync(SearchProductsRequest request)
    {
        if (request == null)
        {
            request = new();
        }
        
        Filter<Product> filter = new()
        {
            Includes = 
            [
                x => x.Category!
            ],
            Predicate = request.GetPredicate(),
            PageNumber = request.PageNumber ?? 1,
            PageSize = request.PageSize ?? 10,
            SortBy = request.SortBy ?? "RegularPrice",
            SortDescending = request.SortDescending ?? false,
        };

        Paginated<Product> result = await productRepository.SearchAsync(filter);

        List<ProductsResponse> responses = new();

        foreach (Product product in result.Items!)
        {
            ProductsResponse response = new()
            {
                Id = product.Id,
                Title = product.Title,
                Description = product.Description,
                MainImageUrl = product.MainImageUrl,
                RegularPrice = product.RegularPrice,
                DiscountPercentage = product.DiscountPercentage,
                DiscountedPrice = product.DiscountedPrice,
                Quantity = product.Quantity,
                Rating = product.Rating,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? string.Empty,
                SecondaryImages = product.SecondaryImages
                    .Select(si => new ImageResponse
                    {
                        Id = si.Id,
                        Uri = si.Uri
                    })
                    .ToList()                
            };

            responses.Add(response);
        }

        Paginated<ProductsResponse> paginated = new()
        {
            Items = responses,
            TotalCount = result.TotalCount
        };

        return paginated;
    }
    
}
