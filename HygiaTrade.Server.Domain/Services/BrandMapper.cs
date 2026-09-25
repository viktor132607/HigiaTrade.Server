using HygiaTrade.Common.Responses.Brand;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Domain.Services;

public interface IBrandMapper
{
    BrandResponse ToResponse(
        Brand brand,
        int productCount);

    BrandResponse ToResponse(
        BrandListItem brand);
}

public sealed class BrandMapper
    : IBrandMapper
{
    public BrandResponse ToResponse(
        Brand brand,
        int productCount) =>
        new(
            brand.Id,
            brand.Name,
            brand.ThumbnailImageUrl,
            brand.Description,
            productCount);

    public BrandResponse ToResponse(
        BrandListItem brand) =>
        new(
            brand.Id,
            brand.Name,
            brand.ThumbnailImageUrl,
            brand.Description,
            brand.ProductCount);
}
