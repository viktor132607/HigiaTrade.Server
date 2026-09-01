using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/distribution-routes")]
public sealed class DistributionRoutesController(
    ApplicationDbContext db,
    IHttpClientFactory httpClientFactory,
    ILogger<DistributionRoutesController> logger) : ControllerBase
{
    private const string ContentKey = "distribution-routes";
    private const string DepotName = "HygiaTrade – Русе";
    private const string DepotAddress = "Русе, България";
    private const double DepotLatitude = 43.8356;
    private const double DepotLongitude = 25.9657;
    private const int MaxOrdersPerRoute = 20;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [HttpGet]
    public async Task<ActionResult<DistributionRoutesPageResponse>> GetAsync(CancellationToken cancellationToken)
    {
        await EnsureStoreAsync(cancellationToken);
        DistributionRouteStore store = await ReadAsync(cancellationToken) ?? new DistributionRouteStore();

        HashSet<Guid> assignedOrderIds = store.Routes
            .SelectMany(route => route.Stops)
            .Select(stop => stop.OrderId)
            .ToHashSet();

        List<Order> orders = await db.Orders
            .AsNoTracking()
            .Where(order =>
                !order.IsDeleted &&
                order.Status != OrderStatus.Delivered &&
                order.Status != OrderStatus.Cancelled)
            .OrderBy(order => order.CreatedOn)
            .ToListAsync(cancellationToken);

        List<DistributionOrderDto> unassignedOrders = orders
            .Where(order => !assignedOrderIds.Contains(order.Id))
            .Select(ToOrderDto)
            .ToList();

        return Ok(new DistributionRoutesPageResponse
        {
            Depot = CreateDepot(),
            Routes = store.Routes
                .OrderByDescending(route => route.RouteDate)
                .ThenByDescending(route => route.CreatedOn)
                .ToList(),
            UnassignedOrders = unassignedOrders
        });
    }

    [HttpPost("optimize")]
    public async Task<ActionResult<DistributionRouteDto>> OptimizeAsync(
        [FromBody] CreateDistributionRouteRequest request,
        CancellationToken cancellationToken)
    {
        string distributorName = (request.DistributorName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(distributorName))
        {
            return BadRequest(new { message = "Въведи име на дистрибутор." });
        }

        List<Guid> requestedIds = (request.OrderIds ?? [])
            .Distinct()
            .ToList();

        if (requestedIds.Count == 0)
        {
            return BadRequest(new { message = "Избери поне една поръчка за маршрута." });
        }

        if (requestedIds.Count > MaxOrdersPerRoute)
        {
            return BadRequest(new { message = $"Един маршрут може да съдържа най-много {MaxOrdersPerRoute} поръчки." });
        }

        await EnsureStoreAsync(cancellationToken);
        DistributionRouteStore store = await ReadAsync(cancellationToken) ?? new DistributionRouteStore();

        HashSet<Guid> assignedOrderIds = store.Routes
            .SelectMany(route => route.Stops)
            .Select(stop => stop.OrderId)
            .ToHashSet();

        List<Guid> alreadyAssigned = requestedIds
            .Where(assignedOrderIds.Contains)
            .ToList();

        if (alreadyAssigned.Count > 0)
        {
            return Conflict(new
            {
                message = "Някои от избраните поръчки вече са включени в друг маршрут.",
                orderIds = alreadyAssigned
            });
        }

        List<Order> orders = await db.Orders
            .AsNoTracking()
            .Where(order => requestedIds.Contains(order.Id) && !order.IsDeleted)
            .ToListAsync(cancellationToken);

        if (orders.Count != requestedIds.Count)
        {
            return BadRequest(new { message = "Една или повече поръчки вече не съществуват." });
        }

        if (orders.Any(order => order.Status is OrderStatus.Delivered or OrderStatus.Cancelled))
        {
            return BadRequest(new { message = "Доставена или отказана поръчка не може да бъде добавена към маршрут." });
        }

        List<LocatedOrder> locatedOrders = [];
        for (int index = 0; index < orders.Count; index++)
        {
            Order order = orders[index];
            string fullAddress = BuildOrderAddress(order);

            if (string.IsNullOrWhiteSpace(fullAddress))
            {
                return UnprocessableEntity(new
                {
                    message = "Поръчката няма достатъчно адресни данни за маршрут.",
                    orderId = order.Id
                });
            }

            GeoPoint? point = await GeocodeOrderAsync(order, cancellationToken);
            if (point is null)
            {
                return UnprocessableEntity(new
                {
                    message = "Адресът на поръчката не можа да бъде намерен на картата.",
                    orderId = order.Id,
                    address = fullAddress
                });
            }

            locatedOrders.Add(new LocatedOrder(order, point));

            // Nominatim's public service asks clients to stay at or below 1 request/second.
            if (index < orders.Count - 1)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(1050), cancellationToken);
            }
        }

        List<LocatedOrder> optimized = OptimizeStopOrder(locatedOrders);
        List<DistributionRouteStopDto> stops = optimized
            .Select((item, index) => ToStopDto(item.Order, item.Point, index + 1))
            .ToList();

        RouteSummary summary = await GetRoadSummaryAsync(stops, cancellationToken)
            ?? CalculateFallbackSummary(stops);

        DateOnly routeDate = request.RouteDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        DistributionRouteDto route = new()
        {
            Id = Guid.NewGuid(),
            DistributorName = distributorName,
            RouteDate = routeDate,
            CreatedOn = DateTime.UtcNow,
            Start = CreateDepot(),
            End = CreateDepot(),
            TotalDistanceKm = Math.Round(summary.DistanceKm, 1),
            EstimatedDurationMinutes = Math.Max(1, (int)Math.Round(summary.DurationMinutes)),
            NavigationUrl = BuildGoogleMapsUrl(stops),
            Stops = stops
        };

        store.Routes.Add(route);
        await WriteAsync(store, cancellationToken);

        return Ok(route);
    }

    [HttpDelete("{routeId:guid}")]
    public async Task<IActionResult> DeleteAsync(Guid routeId, CancellationToken cancellationToken)
    {
        await EnsureStoreAsync(cancellationToken);
        DistributionRouteStore store = await ReadAsync(cancellationToken) ?? new DistributionRouteStore();
        int removed = store.Routes.RemoveAll(route => route.Id == routeId);

        if (removed == 0)
        {
            return NotFound(new { message = "Маршрутът не е намерен." });
        }

        await WriteAsync(store, cancellationToken);
        return NoContent();
    }

    private async Task<GeoPoint?> GeocodeOrderAsync(Order order, CancellationToken cancellationToken)
    {
        string fullAddress = BuildOrderAddress(order);
        GeoPoint? exact = await GeocodeAsync(fullAddress, "address", cancellationToken);
        if (exact is not null)
        {
            return exact;
        }

        string city = (order.City ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        string country = string.IsNullOrWhiteSpace(order.Country) ? "България" : order.Country.Trim();
        return await GeocodeAsync($"{city}, {country}", "city", cancellationToken);
    }

    private async Task<GeoPoint?> GeocodeAsync(
        string query,
        string precision,
        CancellationToken cancellationToken)
    {
        try
        {
            HttpClient client = httpClientFactory.CreateClient();
            string url =
                "https://nominatim.openstreetmap.org/search" +
                $"?format=jsonv2&limit=1&countrycodes=bg&q={Uri.EscapeDataString(query)}";

            using HttpRequestMessage message = new(HttpMethod.Get, url);
            message.Headers.UserAgent.ParseAdd("HygiaTradeRoutePlanner/1.0");
            message.Headers.Accept.ParseAdd("application/json");

            using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Geocoding failed with status {StatusCode} for {Query}.", response.StatusCode, query);
                return null;
            }

            List<NominatimResult>? results = await response.Content
                .ReadFromJsonAsync<List<NominatimResult>>(cancellationToken: cancellationToken);

            NominatimResult? result = results?.FirstOrDefault();
            if (result is null ||
                !double.TryParse(result.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out double latitude) ||
                !double.TryParse(result.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out double longitude))
            {
                return null;
            }

            return new GeoPoint(latitude, longitude, precision);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "Geocoding request failed for {Query}.", query);
            return null;
        }
    }

    private async Task<RouteSummary?> GetRoadSummaryAsync(
        IReadOnlyList<DistributionRouteStopDto> stops,
        CancellationToken cancellationToken)
    {
        if (stops.Count == 0)
        {
            return new RouteSummary(0, 0);
        }

        try
        {
            List<string> coordinates =
            [
                $"{DepotLongitude.ToString(CultureInfo.InvariantCulture)},{DepotLatitude.ToString(CultureInfo.InvariantCulture)}"
            ];

            coordinates.AddRange(stops.Select(stop =>
                $"{stop.Longitude.ToString(CultureInfo.InvariantCulture)},{stop.Latitude.ToString(CultureInfo.InvariantCulture)}"));
            coordinates.Add(
                $"{DepotLongitude.ToString(CultureInfo.InvariantCulture)},{DepotLatitude.ToString(CultureInfo.InvariantCulture)}");

            string url =
                $"https://router.project-osrm.org/route/v1/driving/{string.Join(';', coordinates)}?overview=false&steps=false";

            HttpClient client = httpClientFactory.CreateClient();
            using HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            OsrmResponse? payload = await response.Content
                .ReadFromJsonAsync<OsrmResponse>(cancellationToken: cancellationToken);
            OsrmRoute? route = payload?.Routes?.FirstOrDefault();
            if (route is null)
            {
                return null;
            }

            return new RouteSummary(route.Distance / 1000d, route.Duration / 60d);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "OSRM route summary failed; falling back to local estimate.");
            return null;
        }
    }

    private static List<LocatedOrder> OptimizeStopOrder(IReadOnlyList<LocatedOrder> input)
    {
        List<LocatedOrder> remaining = input.ToList();
        List<LocatedOrder> ordered = [];
        double currentLat = DepotLatitude;
        double currentLon = DepotLongitude;

        while (remaining.Count > 0)
        {
            LocatedOrder next = remaining
                .OrderBy(item => HaversineKm(currentLat, currentLon, item.Point.Latitude, item.Point.Longitude))
                .First();

            ordered.Add(next);
            remaining.Remove(next);
            currentLat = next.Point.Latitude;
            currentLon = next.Point.Longitude;
        }

        // A small 2-opt pass improves the nearest-neighbour result without any paid routing API.
        bool improved = true;
        int passes = 0;
        while (improved && passes < 8 && ordered.Count >= 4)
        {
            improved = false;
            passes++;

            for (int i = 0; i < ordered.Count - 1; i++)
            {
                for (int k = i + 1; k < ordered.Count; k++)
                {
                    double before = RouteLengthKm(ordered);
                    List<LocatedOrder> candidate = TwoOptSwap(ordered, i, k);
                    double after = RouteLengthKm(candidate);

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

    private static List<LocatedOrder> TwoOptSwap(List<LocatedOrder> route, int i, int k)
    {
        List<LocatedOrder> result = [];
        result.AddRange(route.Take(i));
        result.AddRange(route.Skip(i).Take(k - i + 1).Reverse());
        result.AddRange(route.Skip(k + 1));
        return result;
    }

    private static double RouteLengthKm(IReadOnlyList<LocatedOrder> route)
    {
        double total = 0;
        double latitude = DepotLatitude;
        double longitude = DepotLongitude;

        foreach (LocatedOrder stop in route)
        {
            total += HaversineKm(latitude, longitude, stop.Point.Latitude, stop.Point.Longitude);
            latitude = stop.Point.Latitude;
            longitude = stop.Point.Longitude;
        }

        total += HaversineKm(latitude, longitude, DepotLatitude, DepotLongitude);
        return total;
    }

    private static RouteSummary CalculateFallbackSummary(IReadOnlyList<DistributionRouteStopDto> stops)
    {
        double totalKm = 0;
        double latitude = DepotLatitude;
        double longitude = DepotLongitude;

        foreach (DistributionRouteStopDto stop in stops)
        {
            totalKm += HaversineKm(latitude, longitude, stop.Latitude, stop.Longitude);
            latitude = stop.Latitude;
            longitude = stop.Longitude;
        }

        totalKm += HaversineKm(latitude, longitude, DepotLatitude, DepotLongitude);
        double roadEstimateKm = totalKm * 1.22;
        double durationMinutes = roadEstimateKm / 55d * 60d;
        return new RouteSummary(roadEstimateKm, durationMinutes);
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0088;
        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);
        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;

    private static string BuildGoogleMapsUrl(IReadOnlyList<DistributionRouteStopDto> stops)
    {
        string origin = Uri.EscapeDataString("Русе, България");
        string destination = origin;
        string waypoints = string.Join('|', stops.Select(stop => Uri.EscapeDataString(stop.FullAddress)));

        return
            "https://www.google.com/maps/dir/?api=1" +
            $"&origin={origin}" +
            $"&destination={destination}" +
            $"&waypoints={waypoints}" +
            "&travelmode=driving";
    }

    private static DistributionOrderDto ToOrderDto(Order order) => new()
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

    private static DistributionRouteStopDto ToStopDto(Order order, GeoPoint point, int position) => new()
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

    private static string BuildOrderAddress(Order order)
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

    private static DistributionDepotDto CreateDepot() => new()
    {
        Name = DepotName,
        Address = DepotAddress,
        Latitude = DepotLatitude,
        Longitude = DepotLongitude
    };

    private async Task EnsureStoreAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = db.Database.GetDbConnection();
        bool closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS "SiteContent" (
                    "Key" text PRIMARY KEY,
                    "Value" jsonb NOT NULL,
                    "ModifiedOn" timestamp with time zone NOT NULL DEFAULT NOW()
                );
                """;
            await command.ExecuteNonQueryAsync(cancellationToken);

            await using DbCommand seedCommand = connection.CreateCommand();
            seedCommand.CommandText = """
                INSERT INTO "SiteContent" ("Key", "Value", "ModifiedOn")
                VALUES (@key, CAST(@value AS jsonb), NOW())
                ON CONFLICT ("Key") DO NOTHING;
                """;
            AddParameter(seedCommand, "@key", ContentKey);
            AddParameter(seedCommand, "@value", JsonSerializer.Serialize(new DistributionRouteStore(), JsonOptions));
            await seedCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (closeAfter)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<DistributionRouteStore?> ReadAsync(CancellationToken cancellationToken)
    {
        DbConnection connection = db.Database.GetDbConnection();
        bool closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT \"Value\"::text FROM \"SiteContent\" WHERE \"Key\" = @key LIMIT 1;";
            AddParameter(command, "@key", ContentKey);
            object? result = await command.ExecuteScalarAsync(cancellationToken);
            string? json = result?.ToString();
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<DistributionRouteStore>(json, JsonOptions);
        }
        finally
        {
            if (closeAfter)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task WriteAsync(DistributionRouteStore store, CancellationToken cancellationToken)
    {
        DbConnection connection = db.Database.GetDbConnection();
        bool closeAfter = connection.State != ConnectionState.Open;
        if (closeAfter)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO "SiteContent" ("Key", "Value", "ModifiedOn")
                VALUES (@key, CAST(@value AS jsonb), NOW())
                ON CONFLICT ("Key") DO UPDATE SET
                    "Value" = EXCLUDED."Value",
                    "ModifiedOn" = NOW();
                """;
            AddParameter(command, "@key", ContentKey);
            AddParameter(command, "@value", JsonSerializer.Serialize(store, JsonOptions));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (closeAfter)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed record LocatedOrder(Order Order, GeoPoint Point);
    private sealed record GeoPoint(double Latitude, double Longitude, string Precision);
    private sealed record RouteSummary(double DistanceKm, double DurationMinutes);
}

public sealed class CreateDistributionRouteRequest
{
    public string? DistributorName { get; set; }
    public DateOnly? RouteDate { get; set; }
    public List<Guid>? OrderIds { get; set; }
}

public sealed class DistributionRoutesPageResponse
{
    public DistributionDepotDto Depot { get; set; } = new();
    public List<DistributionRouteDto> Routes { get; set; } = [];
    public List<DistributionOrderDto> UnassignedOrders { get; set; } = [];
}

public sealed class DistributionRouteStore
{
    public List<DistributionRouteDto> Routes { get; set; } = [];
}

public sealed class DistributionRouteDto
{
    public Guid Id { get; set; }
    public string DistributorName { get; set; } = string.Empty;
    public DateOnly RouteDate { get; set; }
    public DateTime CreatedOn { get; set; }
    public DistributionDepotDto Start { get; set; } = new();
    public DistributionDepotDto End { get; set; } = new();
    public double TotalDistanceKm { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string NavigationUrl { get; set; } = string.Empty;
    public List<DistributionRouteStopDto> Stops { get; set; } = [];
}

public sealed class DistributionDepotDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public sealed class DistributionOrderDto
{
    public Guid Id { get; set; }
    public string Names { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public int Status { get; set; }
    public decimal OrderTotalPrice { get; set; }
}

public sealed class DistributionRouteStopDto
{
    public Guid OrderId { get; set; }
    public int Position { get; set; }
    public string Names { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public decimal OrderTotalPrice { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string GeocodingPrecision { get; set; } = string.Empty;
}

public sealed class NominatimResult
{
    [JsonPropertyName("lat")]
    public string Lat { get; set; } = string.Empty;

    [JsonPropertyName("lon")]
    public string Lon { get; set; } = string.Empty;
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
