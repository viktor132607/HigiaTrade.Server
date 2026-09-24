using HygiaTrade.API.Services;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class DistributionRouteMappingTests
{
    [Fact]
    public void Mapping_UsesEmptyStrings_ForNullOrderFields()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Names = null,
            Phone = null,
            Address = null,
            City = null,
            PostalCode = null,
            Country = null
        };

        var orderDto =
            DistributionRouteMapping.ToOrderDto(order);

        var stopDto =
            DistributionRouteMapping.ToStopDto(
                order,
                new DistributionGeoPoint(1, 2, "city"),
                3);

        Assert.Equal(string.Empty, orderDto.Names);
        Assert.Equal(string.Empty, orderDto.Phone);
        Assert.Equal(string.Empty, orderDto.Address);
        Assert.Equal(string.Empty, orderDto.City);
        Assert.Equal(string.Empty, orderDto.PostalCode);
        Assert.Equal(string.Empty, orderDto.Country);
        Assert.Equal(string.Empty, orderDto.FullAddress);

        Assert.Equal(string.Empty, stopDto.Names);
        Assert.Equal(string.Empty, stopDto.Phone);
        Assert.Equal(string.Empty, stopDto.Address);
        Assert.Equal(string.Empty, stopDto.City);
        Assert.Equal(string.Empty, stopDto.PostalCode);
        Assert.Equal(string.Empty, stopDto.Country);
        Assert.Equal(string.Empty, stopDto.FullAddress);
        Assert.Equal(3, stopDto.Position);
    }

    [Fact]
    public void BuildOrderAddress_TrimsAndSkipsWhitespace()
    {
        string result =
            DistributionRouteMapping.BuildOrderAddress(
                new Order
                {
                    Address = "  Main 1 ",
                    PostalCode = " ",
                    City = " Ruse ",
                    Country = " Bulgaria "
                });

        Assert.Equal("Main 1, Ruse, Bulgaria", result);
    }
}
