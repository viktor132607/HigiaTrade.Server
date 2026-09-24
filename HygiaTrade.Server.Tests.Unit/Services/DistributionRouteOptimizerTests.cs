using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class DistributionRouteOptimizerTests
{
    private readonly DistributionRouteOptimizer optimizer = new();

    [Fact]
    public void Optimize_ReturnsEmpty_ForEmptyInput()
    {
        IReadOnlyList<LocatedDistributionOrder> result =
            optimizer.Optimize(
                Array.Empty<LocatedDistributionOrder>());

        Assert.Empty(result);
    }

    [Fact]
    public void Optimize_UsesNearestNeighbourFromDepot()
    {
        Order near = new() { Id = Guid.NewGuid() };
        Order far = new() { Id = Guid.NewGuid() };

        var input = new[]
        {
            new LocatedDistributionOrder(
                far,
                new DistributionGeoPoint(
                    44.5,
                    26.5,
                    "address")),
            new LocatedDistributionOrder(
                near,
                new DistributionGeoPoint(
                    DistributionRouteMapping.DepotLatitude + 0.001,
                    DistributionRouteMapping.DepotLongitude + 0.001,
                    "address"))
        };

        IReadOnlyList<LocatedDistributionOrder> result =
            optimizer.Optimize(input);

        Assert.Equal(near.Id, result[0].Order.Id);
        Assert.Equal(far.Id, result[1].Order.Id);
    }

    [Fact]
    public void Optimize_PreservesAllStops_DuringTwoOptPasses()
    {
        var input = new[]
        {
            Located(43.90, 26.10),
            Located(43.70, 26.20),
            Located(43.95, 25.80),
            Located(43.75, 25.70)
        };

        Guid[] expectedIds =
            input.Select(item => item.Order.Id)
                .OrderBy(id => id)
                .ToArray();

        IReadOnlyList<LocatedDistributionOrder> result =
            optimizer.Optimize(input);

        Guid[] actualIds =
            result.Select(item => item.Order.Id)
                .OrderBy(id => id)
                .ToArray();

        Assert.Equal(expectedIds, actualIds);
    }

    [Fact]
    public void CalculateFallbackSummary_ReturnsZero_ForNoStops()
    {
        DistributionRouteSummary result =
            optimizer.CalculateFallbackSummary(
                Array.Empty<DistributionRouteStopDto>());

        Assert.Equal(0, result.DistanceKm);
        Assert.Equal(0, result.DurationMinutes);
    }

    [Fact]
    public void CalculateFallbackSummary_ReturnsPositiveRoadEstimate()
    {
        DistributionRouteSummary result =
            optimizer.CalculateFallbackSummary(
            [
                new DistributionRouteStopDto
                {
                    Latitude =
                        DistributionRouteMapping.DepotLatitude + 0.1,
                    Longitude =
                        DistributionRouteMapping.DepotLongitude + 0.1
                }
            ]);

        Assert.True(result.DistanceKm > 0);
        Assert.True(result.DurationMinutes > 0);
    }

    private static LocatedDistributionOrder Located(
        double latitude,
        double longitude) =>
        new(
            new Order
            {
                Id = Guid.NewGuid()
            },
            new DistributionGeoPoint(
                latitude,
                longitude,
                "address"));
}
