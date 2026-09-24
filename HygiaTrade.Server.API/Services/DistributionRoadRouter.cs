using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HygiaTrade.API.Controllers;

namespace HygiaTrade.API.Services;

public sealed record DistributionRouteSummary(
    double DistanceKm,
    double DurationMinutes);

public interface IDistributionRoadRouter
{
    Task<DistributionRouteSummary?> GetSummaryAsync(
        IReadOnlyList<DistributionRouteStopDto> stops,
        CancellationToken cancellationToken);
}

public sealed class DistributionRoadRouter(
    IHttpClientFactory httpClientFactory,
    ILogger<DistributionRoadRouter> logger)
    : IDistributionRoadRouter
{
    public async Task<DistributionRouteSummary?> GetSummaryAsync(
        IReadOnlyList<DistributionRouteStopDto> stops,
        CancellationToken cancellationToken)
    {
        if (stops.Count == 0)
        {
            return new DistributionRouteSummary(0, 0);
        }

        try
        {
            List<string> coordinates =
            [
                FormatCoordinate(
                    DistributionRouteMapping.DepotLongitude,
                    DistributionRouteMapping.DepotLatitude)
            ];

            coordinates.AddRange(
                stops.Select(stop =>
                    FormatCoordinate(
                        stop.Longitude,
                        stop.Latitude)));

            coordinates.Add(
                FormatCoordinate(
                    DistributionRouteMapping.DepotLongitude,
                    DistributionRouteMapping.DepotLatitude));

            string url =
                "https://router.project-osrm.org/route/v1/driving/" +
                $"{string.Join(';', coordinates)}" +
                "?overview=false&steps=false";

            HttpClient client =
                httpClientFactory.CreateClient();

            using HttpResponseMessage response =
                await client.GetAsync(
                    url,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            OsrmResponse? payload =
                await response.Content.ReadFromJsonAsync<OsrmResponse>(
                    cancellationToken: cancellationToken);

            OsrmRoute? route =
                payload?.Routes?.FirstOrDefault();

            if (route is null)
            {
                return null;
            }

            return new DistributionRouteSummary(
                route.Distance / 1000d,
                route.Duration / 60d);
        }
        catch (Exception exception)
            when (exception is
                HttpRequestException or
                TaskCanceledException or
                JsonException)
        {
            logger.LogWarning(
                exception,
                "OSRM route summary failed; falling back to local estimate.");

            return null;
        }
    }

    private static string FormatCoordinate(
        double longitude,
        double latitude) =>
        $"{longitude.ToString(CultureInfo.InvariantCulture)}," +
        $"{latitude.ToString(CultureInfo.InvariantCulture)}";
}

public sealed class OsrmResponse
{
    [JsonPropertyName("routes")]
    public List<OsrmRoute> Routes { get; set; } = [];
}

public sealed class OsrmRoute
{
    [JsonPropertyName("distance")]
    public double Distance { get; set; }

    [JsonPropertyName("duration")]
    public double Duration { get; set; }
}
