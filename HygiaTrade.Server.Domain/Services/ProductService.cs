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
using HygiaTrade.Domain.Pricing;

namespace HygiaTrade.Domain.Services;

public class ProductService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IImageRepository imageRepository) : IProductService
{
    public async Task<IEnumerable<ProductResponse>?> GetAsync()
    {
        IEnumerable<Product> products = await productRepository.GetAllAsync();
        return products.Select(ToProductResponse);
    }

    public async Task<IEnumerable<ProductResponse>?> GetBestSellersAsync(int numOfBestSellers)
    {
        IEnumerable<Product> products = await productRepository.GetBestSellersAsync(numOfBestSellers);
        return products.Select(ToProductResponse);
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id)
    {
        Product? product = await productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new AppException("Product not found.").SetStatusCode(404);
        }

        return ToProductResponse(product);
    }

    public async Task<ProductPriceQuoteResponse> GetPriceQuoteAsync(Guid id, int quantity)
    {
        if (quantity <= 0)
        {
            throw new AppException("Quantity must be greater than zero.").SetStatusCode(400);
        }

        Product? product = await productRepository.GetByIdAsync(id);
        if (product == null)
        {
            throw new AppException("Product not found.").SetStatusCode(404);
        }

        ProductPriceBreakdown pricing = ProductPricingCalculator.Calculate(product, quantity);
        decimal totalInclVat = ProductPricingCalculator.RoundMoney(pricing.UnitPriceInclVat * quantity);
        decimal totalExclVat = ProductPricingCalculator.GrossToNet(totalInclVat, pricing.VatRate);
        decimal vatAmount = ProductPricingCalculator.RoundMoney(totalInclVat - totalExclVat);

        return new ProductPriceQuoteResponse
        {
            ProductId = product.Id,
            Quantity = quantity,
            PricingTier = pricing.PricingTier.ToString(),
            WholesaleMinQuantity = product.WholesaleMinQuantity,
            VatRate = pricing.VatRate,
            UnitPriceExclVat = pricing.UnitPriceExclVat,
            UnitPriceInclVat = pricing.UnitPriceInclVat,
            TotalExclVat = totalExclVat,
            VatAmount = vatAmount,
            TotalInclVat = totalInclVat,
        };
    }

    public async Task<ProductResponse?> CreateAsync(CreateProductRequest request)
    {
        Category? category = await categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null)
        {
            throw new AppException("Invalid category.").SetStatusCode(400);
        }

        ValidatePricing(
            request.RegularPrice,
            request.DiscountPercentage,
            request.DiscountedPrice,
            request.WholesalePrice,
            request.WholesaleMinQuantity,
            request.VatRate);

        Product product = new()
        {
            Title = request.Title,
            Brand = NormalizeBrand(request.Brand),
            Description = request.Description,
            MainImageUrl = request.MainImageUrl,
            IsActive = request.IsActive,
            // Ratings come only from real reviews; inventory comes only from stock receipts.
            Rating = 0,
            Quantity = 0,
            CategoryId = request.CategoryId,
            WholesalePrice = ProductPricingCalculator.RoundMoney(request.WholesalePrice),
            WholesaleMinQuantity = request.WholesaleMinQuantity,
            VatRate = request.VatRate,
        };

        ApplyRetailPricing(
            product,
            request.RegularPrice,
            request.DiscountPercentage,
            request.DiscountedPrice);

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

        product.Category = category;
        product.SecondaryImages = images;

        return ToProductResponse(product);
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

        decimal wholesalePrice = request.WholesalePrice ?? existingProduct.WholesalePrice;
        uint wholesaleMinQuantity = request.WholesaleMinQuantity ?? existingProduct.WholesaleMinQuantity;
        decimal vatRate = request.VatRate ?? existingProduct.VatRate;

        ValidatePricing(
            request.RegularPrice,
            request.DiscountPercentage,
            request.DiscountedPrice,
            wholesalePrice,
            wholesaleMinQuantity,
            vatRate);

        existingProduct.Title = request.Title;
        existingProduct.Brand = NormalizeBrand(request.Brand);
        existingProduct.Description = request.Description;
        existingProduct.MainImageUrl = request.MainImageUrl;
        existingProduct.IsActive = request.IsActive;
        // Quantity is deliberately not editable here. InventoryController owns stock changes.
        existingProduct.CategoryId = request.CategoryId;
        existingProduct.WholesalePrice = ProductPricingCalculator.RoundMoney(wholesalePrice);
        existingProduct.WholesaleMinQuantity = wholesaleMinQuantity;
        existingProduct.VatRate = vatRate;

        ApplyRetailPricing(
            existingProduct,
            request.RegularPrice,
            request.DiscountPercentage,
            request.DiscountedPrice);

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

        updatedProduct.Category = category;
        return ToProductResponse(updatedProduct);
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
        request ??= new SearchProductsRequest();

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

        return new Paginated<ProductsResponse>
        {
            Items = result.Items?.Select(ToProductsResponse).ToList() ?? new List<ProductsResponse>(),
            TotalCount = result.TotalCount
        };
    }

    private static string? NormalizeBrand(string? brand)
    {
        var normalized = brand?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static void ValidatePricing(
        decimal regularPrice,
        byte discountPercentage,
        decimal discountedPrice,
        decimal wholesalePrice,
        uint wholesaleMinQuantity,
        decimal vatRate)
    {
        if (regularPrice < 0m)
        {
            throw new AppException("Retail price cannot be negative.").SetStatusCode(400);
        }

        if (discountPercentage > 100)
        {
            throw new AppException("Discount percentage must be between 0 and 100.").SetStatusCode(400);
        }

        if (discountedPrice < 0m)
        {
            throw new AppException("Discounted price cannot be negative.").SetStatusCode(400);
        }

        if (discountedPrice > 0m && regularPrice <= 0m)
        {
            throw new AppException("A positive retail price is required when a discounted price is set.").SetStatusCode(400);
        }

        if (discountedPrice > regularPrice && regularPrice > 0m)
        {
            throw new AppException("Discounted price cannot exceed the retail price.").SetStatusCode(400);
        }

        if (wholesalePrice < 0m)
        {
            throw new AppException("Wholesale price cannot be negative.").SetStatusCode(400);
        }

        bool wholesalePriceConfigured = wholesalePrice > 0m;
        bool wholesaleQuantityConfigured = wholesaleMinQuantity > 0;

        if (wholesalePriceConfigured != wholesaleQuantityConfigured)
        {
            throw new AppException("Wholesale price and minimum quantity must either both be configured or both be zero.").SetStatusCode(400);
        }

        try
        {
            ProductPricingCalculator.ValidateVatRate(vatRate);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new AppException("VAT rate must be between 0 and 100 percent.").SetStatusCode(400);
        }
    }

    private static void ApplyRetailPricing(
        Product product,
        decimal regularPrice,
        byte discountPercentage,
        decimal discountedPrice)
    {
        product.RegularPrice = ProductPricingCalculator.RoundMoney(regularPrice);

        if (discountedPrice > 0m)
        {
            product.DiscountedPrice = ProductPricingCalculator.RoundMoney(discountedPrice);
            product.DiscountPercentage = product.RegularPrice == 0m
                ? (byte)0
                : (byte)Math.Clamp(
                    (int)Math.Round(
                        (1m - product.DiscountedPrice / product.RegularPrice) * 100m,
                        0,
                        MidpointRounding.AwayFromZero),
                    0,
                    100);

            return;
        }

        product.DiscountPercentage = discountPercentage;
        product.DiscountedPrice = discountPercentage == 0
            ? 0m
            : ProductPricingCalculator.RoundMoney(
                product.RegularPrice * (1m - discountPercentage / 100m));
    }

    private static ProductResponse ToProductResponse(Product product)
    {
        decimal retailExclVat = ProductPricingCalculator.GrossToNet(product.RegularPrice, product.VatRate);
        decimal discountedExclVat = ProductPricingCalculator.GrossToNet(product.DiscountedPrice, product.VatRate);
        decimal wholesaleExclVat = ProductPricingCalculator.GrossToNet(product.WholesalePrice, product.VatRate);
        bool wholesaleEnabled = product.WholesalePrice > 0m && product.WholesaleMinQuantity > 0;

        return new ProductResponse
        {
            Id = product.Id,
            Title = product.Title,
            Brand = product.Brand,
            Description = product.Description,
            MainImageUrl = product.MainImageUrl,
            IsActive = product.IsActive,
            RegularPrice = product.RegularPrice,
            DiscountPercentage = product.DiscountPercentage,
            DiscountedPrice = product.DiscountedPrice,
            RetailPriceInclVat = product.RegularPrice,
            RetailPriceExclVat = retailExclVat,
            DiscountedPriceInclVat = product.DiscountedPrice,
            DiscountedPriceExclVat = discountedExclVat,
            WholesalePriceInclVat = product.WholesalePrice,
            WholesalePriceExclVat = wholesaleExclVat,
            WholesaleMinQuantity = product.WholesaleMinQuantity,
            VatRate = product.VatRate,
            WholesaleEnabled = wholesaleEnabled,
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

    private static ProductsResponse ToProductsResponse(Product product)
    {
        decimal retailExclVat = ProductPricingCalculator.GrossToNet(product.RegularPrice, product.VatRate);
        decimal discountedExclVat = ProductPricingCalculator.GrossToNet(product.DiscountedPrice, product.VatRate);
        decimal wholesaleExclVat = ProductPricingCalculator.GrossToNet(product.WholesalePrice, product.VatRate);
        bool wholesaleEnabled = product.WholesalePrice > 0m && product.WholesaleMinQuantity > 0;

        return new ProductsResponse
        {
            Id = product.Id,
            Title = product.Title,
            Brand = product.Brand,
            Description = product.Description,
            MainImageUrl = product.MainImageUrl,
            IsActive = product.IsActive,
            RegularPrice = product.RegularPrice,
            DiscountPercentage = product.DiscountPercentage,
            DiscountedPrice = product.DiscountedPrice,
            RetailPriceInclVat = product.RegularPrice,
            RetailPriceExclVat = retailExclVat,
            DiscountedPriceInclVat = product.DiscountedPrice,
            DiscountedPriceExclVat = discountedExclVat,
            WholesalePriceInclVat = product.WholesalePrice,
            WholesalePriceExclVat = wholesaleExclVat,
            WholesaleMinQuantity = product.WholesaleMinQuantity,
            VatRate = product.VatRate,
            WholesaleEnabled = wholesaleEnabled,
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
    }
}
