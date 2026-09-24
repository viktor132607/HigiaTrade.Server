using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.API.Services;

public sealed record DistributionGeoPoint(
    double Latitude,
    double Longitude,
    string Precision);

public interface IDistributionRouteGeocoder
{
    Task<DistributionGeoPoint?> GeocodeOrderAsync(
        Order order,
        CancellationToken cancellationToken);
}

public sealed class DistributionRouteGeocoder(
    IHttpClientFactory httpClientFactory,
    ILogger<DistributionRouteGeocoder> logger)
    : IDistributionRouteGeocoder
{
    public async Task<DistributionGeoPoint?> GeocodeOrderAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        string fullAddress =
            DistributionRouteMapping.BuildOrderAddress(order);

        DistributionGeoPoint? exact =
            await GeocodeAsync(
                fullAddress,
                "address",
                cancellationToken);

        if (exact is not null)
        {
            return exact;
        }

        string city = (order.City ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        string country = string.IsNullOrWhiteSpace(order.Country)
            ? "България"
            : order.Country.Trim();

        return await GeocodeAsync(
            $"{city}, {country}",
            "city",
            cancellationToken);
    }

    private async Task<DistributionGeoPoint?> GeocodeAsync(
        string query,
        string precision,
        CancellationToken cancellationToken)
    {
        try
        {
            HttpClient client =
                httpClientFactory.CreateClient();

            string url =
                "https://nominatim.openstreetmap.org/search" +
                "?format=jsonv2&limit=1&countrycodes=bg&q=" +
                Uri.EscapeDataString(query);

            using var message =
                new HttpRequestMessage(HttpMethod.Get, url);

            message.Headers.UserAgent.ParseAdd(
                "HygiaTradeRoutePlanner/1.0");

            message.Headers.Accept.ParseAdd(
                "application/json");

            using HttpResponseMessage response =
                await client.SendAsync(
                    message,
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Geocoding failed with status {StatusCode} for {Query}.",
                    response.StatusCode,
                    query);

                return null;
            }

            List<NominatimResult>? results =
                await response.Content.ReadFromJsonAsync<
                    List<NominatimResult>>(
                    cancellationToken: cancellationToken);

            NominatimResult? result =
                results?.FirstOrDefault();

            if (result is null ||
                !double.TryParse(
                    result.Lat,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double latitude) ||
                !double.TryParse(
                    result.Lon,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out double longitude))
            {
                return null;
            }

            return new DistributionGeoPoint(
                latitude,
                longitude,
                precision);
        }
        catch (Exception exception)
            when (exception is
                HttpRequestException or
                TaskCanceledException or
                JsonException)
        {
            logger.LogWarning(
                exception,
                "Geocoding request failed for {Query}.",
                query);

            return null;
        }
    }
}

public sealed class NominatimResult
{
    [JsonPropertyName("lat")]
    public string Lat { get; set; } = string.Empty;

    [JsonPropertyName("lon")]
    public string Lon { get; set; } = string.Empty;
}
