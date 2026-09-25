using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class BrandMapperTests
{
    [Fact]
    public void ToResponse_MapsEntityAndProductCount()
    {
        Brand brand =
            new()
            {
                Name = "Acme",
                ThumbnailImageUrl = "image",
                Description = "desc"
            };

        var mapper =
            new BrandMapper();

        var result =
            mapper.ToResponse(
                brand,
                7);

        Assert.Equal(brand.Id, result.Id);
        Assert.Equal("Acme", result.Name);
        Assert.Equal("image", result.ThumbnailImageUrl);
        Assert.Equal("desc", result.Description);
        Assert.Equal(7, result.ProductCount);
    }

    [Fact]
    public void ToResponse_MapsListItem()
    {
        BrandListItem item =
            new(
                Guid.NewGuid(),
                "Acme",
                null,
                null,
                3);

        var mapper =
            new BrandMapper();

        var result =
            mapper.ToResponse(item);

        Assert.Equal(item.Id, result.Id);
        Assert.Equal(item.Name, result.Name);
        Assert.Equal(3, result.ProductCount);
    }
}
