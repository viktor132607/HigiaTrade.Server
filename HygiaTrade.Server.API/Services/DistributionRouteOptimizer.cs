using HygiaTrade.API.Controllers;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.API.Services;

public sealed record LocatedDistributionOrder(
    Order Order,
    DistributionGeoPoint Point);

public interface IDistributionRouteOptimizer
{
    IReadOnlyList<LocatedDistributionOrder> Optimize(
        IReadOnlyList<LocatedDistributionOrder> input);

    DistributionRouteSummary CalculateFallbackSummary(
        IReadOnlyList<DistributionRouteStopDto> stops);
}

public sealed class DistributionRouteOptimizer
    : IDistributionRouteOptimizer
{
    public IReadOnlyList<LocatedDistributionOrder> Optimize(
        IReadOnlyList<LocatedDistributionOrder> input)
    {
        List<LocatedDistributionOrder> remaining =
            input.ToList();

        List<LocatedDistributionOrder> ordered = [];

        double currentLat =
            DistributionRouteMapping.DepotLatitude;

        double currentLon =
            DistributionRouteMapping.DepotLongitude;

        while (remaining.Count > 0)
        {
            LocatedDistributionOrder next = remaining
                .OrderBy(item => HaversineKm(
                    currentLat,
                    currentLon,
                    item.Point.Latitude,
                    item.Point.Longitude))
                .First();

            ordered.Add(next);
            remaining.Remove(next);

            currentLat = next.Point.Latitude;
            currentLon = next.Point.Longitude;
        }

        bool improved = true;
        int passes = 0;

        while (improved &&
               passes < 8 &&
               ordered.Count >= 4)
        {
            improved = false;
            passes++;

            for (int i = 0; i < ordered.Count - 1; i++)
            {
                for (int k = i + 1; k < ordered.Count; k++)
                {
                    double before = RouteLengthKm(ordered);

                    List<LocatedDistributionOrder> candidate =
                        TwoOptSwap(ordered, i, k);

                    double after =
                        RouteLengthKm(candidate);

                    if (after + 0.05 < before)
                    {
                        ordered = candidate;
                        improved = true;
                    }
                }
            }
        }

        return ordered;
    }

    public DistributionRouteSummary CalculateFallbackSummary(
        IReadOnlyList<DistributionRouteStopDto> stops)
    {
        double totalKm = 0;

        double latitude =
            DistributionRouteMapping.DepotLatitude;

        double longitude =
            DistributionRouteMapping.DepotLongitude;

        foreach (DistributionRouteStopDto stop in stops)
        {
            totalKm += HaversineKm(
                latitude,
                longitude,
                stop.Latitude,
                stop.Longitude);

            latitude = stop.Latitude;
            longitude = stop.Longitude;
        }

        totalKm += HaversineKm(
            latitude,
            longitude,
            DistributionRouteMapping.DepotLatitude,
            DistributionRouteMapping.DepotLongitude);

        double roadEstimateKm = totalKm * 1.22;
        double durationMinutes =
            roadEstimateKm / 55d * 60d;

        return new DistributionRouteSummary(
            roadEstimateKm,
            durationMinutes);
    }

    private static List<LocatedDistributionOrder> TwoOptSwap(
        List<LocatedDistributionOrder> route,
        int i,
        int k)
    {
        List<LocatedDistributionOrder> result = [];

        result.AddRange(route.Take(i));

        result.AddRange(
            route
                .Skip(i)
                .Take(k - i + 1)
                .Reverse());

        result.AddRange(route.Skip(k + 1));

        return result;
    }

    private static double RouteLengthKm(
        IReadOnlyList<LocatedDistributionOrder> route)
    {
        double total = 0;

        double latitude =
            DistributionRouteMapping.DepotLatitude;

        double longitude =
            DistributionRouteMapping.DepotLongitude;

        foreach (LocatedDistributionOrder stop in route)
        {
            total += HaversineKm(
                latitude,
                longitude,
                stop.Point.Latitude,
                stop.Point.Longitude);

            latitude = stop.Point.Latitude;
            longitude = stop.Point.Longitude;
        }

        total += HaversineKm(
            latitude,
            longitude,
            DistributionRouteMapping.DepotLatitude,
            DistributionRouteMapping.DepotLongitude);

        return total;
    }

    private static double HaversineKm(
        double lat1,
        double lon1,
        double lat2,
        double lon2)
    {
        const double earthRadiusKm = 6371.0088;

        double dLat =
            DegreesToRadians(lat2 - lat1);

        double dLon =
            DegreesToRadians(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) *
            Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) *
            Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);

        double c =
            2 *
            Math.Atan2(
                Math.Sqrt(a),
                Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(
        double degrees) =>
        degrees * Math.PI / 180d;
}
