using HygiaTrade.API.Controllers;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.API.Services;

public static class DistributionRouteMapping
{
    public const string DepotName = "HygiaTrade – Русе";
    public const string DepotAddress = "Русе, България";
    public const double DepotLatitude = 43.8356;
    public const double DepotLongitude = 25.9657;

    public static DistributionOrderDto ToOrderDto(Order order) =>
        new()
        {
            Id = order.Id,
            Names = order.Names ?? string.Empty,
            Phone = order.Phone ?? string.Empty,
            Address = order.Address ?? string.Empty,
            City = order.City ?? string.Empty,
            PostalCode = order.PostalCode ?? string.Empty,
            Country = order.Country ?? string.Empty,
            FullAddress = BuildOrderAddress(order),
            CreatedOn = order.CreatedOn,
            Status = (int)order.Status,
            OrderTotalPrice = order.OrderTotalPrice
        };

    public static DistributionRouteStopDto ToStopDto(
        Order order,
        DistributionGeoPoint point,
        int position) =>
        new()
        {
            OrderId = order.Id,
            Position = position,
            Names = order.Names ?? string.Empty,
            Phone = order.Phone ?? string.Empty,
            Address = order.Address ?? string.Empty,
            City = order.City ?? string.Empty,
            PostalCode = order.PostalCode ?? string.Empty,
            Country = order.Country ?? string.Empty,
            FullAddress = BuildOrderAddress(order),
            CreatedOn = order.CreatedOn,
            OrderTotalPrice = order.OrderTotalPrice,
            Latitude = point.Latitude,
            Longitude = point.Longitude,
            GeocodingPrecision = point.Precision
        };

    public static string BuildOrderAddress(Order order)
    {
        IEnumerable<string> parts = new[]
            {
                order.Address,
                order.PostalCode,
                order.City,
                order.Country
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim());

        return string.Join(", ", parts);
    }

    public static DistributionDepotDto CreateDepot() =>
        new()
        {
            Name = DepotName,
            Address = DepotAddress,
            Latitude = DepotLatitude,
            Longitude = DepotLongitude
        };

    public static string BuildGoogleMapsUrl(
        IReadOnlyList<DistributionRouteStopDto> stops)
    {
        string origin =
            Uri.EscapeDataString(DepotAddress);

        string waypoints = string.Join(
            '|',
            stops.Select(stop =>
                Uri.EscapeDataString(stop.FullAddress)));

        return
            "https://www.google.com/maps/dir/?api=1" +
            $"&origin={origin}" +
            $"&destination={origin}" +
            $"&waypoints={waypoints}" +
            "&travelmode=driving";
    }
}
