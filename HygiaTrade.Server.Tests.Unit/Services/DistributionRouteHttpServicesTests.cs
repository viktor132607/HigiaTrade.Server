using System.Net.Http;
using System.Net;
using System.Text;
using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using HygiaTrade.Data.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class DistributionRouteHttpServicesTests
{
    [Fact]
    public async Task Geocoder_ReturnsExactAddressResult()
    {
        HttpRequestMessage? request = null;

        HttpClient client = CreateClient(message =>
        {
            request = message;

            return JsonResponse(
                """
                [{"lat":"43.8356","lon":"25.9657"}]
                """);
        });

        DistributionRouteGeocoder geocoder =
            CreateGeocoder(client);

        DistributionGeoPoint? result =
            await geocoder.GeocodeOrderAsync(
                CreateOrder(),
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(43.8356, result.Latitude);
        Assert.Equal(25.9657, result.Longitude);
        Assert.Equal("address", result.Precision);
        Assert.Contains("nominatim.openstreetmap.org", request!.RequestUri!.Host);
        Assert.Contains("HygiaTradeRoutePlanner", request.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task Geocoder_FallsBackToCityAndDefaultCountry()
    {
        int calls = 0;
        List<string> urls = [];

        HttpClient client = CreateClient(message =>
        {
            calls++;
            urls.Add(message.RequestUri!.ToString());

            return calls == 1
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : JsonResponse(
                    """
                    [{"lat":"43.84","lon":"25.96"}]
                    """);
        });

        DistributionRouteGeocoder geocoder =
            CreateGeocoder(client);

        var order = new Order
        {
            Address = "Unknown street",
            City = "Русе",
            Country = null
        };

        DistributionGeoPoint? result =
            await geocoder.GeocodeOrderAsync(
                order,
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("city", result.Precision);
        Assert.Equal(2, calls);
        Assert.Contains(
            Uri.EscapeDataString("Русе, България"),
            urls[1]);
    }

    [Fact]
    public async Task Geocoder_ReturnsNull_WhenCoordinatesAreInvalidAndCityMissing()
    {
        HttpClient client = CreateClient(_ =>
            JsonResponse(
                """
                [{"lat":"bad","lon":"25.96"}]
                """));

        DistributionRouteGeocoder geocoder =
            CreateGeocoder(client);

        DistributionGeoPoint? result =
            await geocoder.GeocodeOrderAsync(
                new Order
                {
                    Address = "Unknown"
                },
                CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Geocoder_ReturnsNull_OnHttpException()
    {
        HttpClient client =
            new(new DelegateHandler(_ =>
                throw new HttpRequestException("network")));

        DistributionRouteGeocoder geocoder =
            CreateGeocoder(client);

        DistributionGeoPoint? result =
            await geocoder.GeocodeOrderAsync(
                new Order
                {
                    Address = "Unknown"
                },
                CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RoadRouter_ReturnsZero_ForNoStops()
    {
        int calls = 0;

        DistributionRoadRouter router =
            CreateRouter(CreateClient(_ =>
            {
                calls++;
                return JsonResponse("{}");
            }));

        DistributionRouteSummary? result =
            await router.GetSummaryAsync(
                Array.Empty<DistributionRouteStopDto>(),
                CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, result.DistanceKm);
        Assert.Equal(0, result.DurationMinutes);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task RoadRouter_ConvertsMetersAndSeconds()
    {
        HttpRequestMessage? request = null;

        DistributionRoadRouter router =
            CreateRouter(CreateClient(message =>
            {
                request = message;

                return JsonResponse(
                    """
                    {"routes":[{"distance":12500,"duration":3600}]}
                    """);
            }));

        DistributionRouteSummary? result =
            await router.GetSummaryAsync(
            [
                new DistributionRouteStopDto
                {
                    Latitude = 43.84,
                    Longitude = 25.96
                }
            ],
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(12.5, result.DistanceKm);
        Assert.Equal(60, result.DurationMinutes);
        Assert.Contains(
            "router.project-osrm.org",
            request!.RequestUri!.Host);
    }

    [Fact]
    public async Task RoadRouter_ReturnsNull_OnNonSuccessStatus()
    {
        DistributionRoadRouter router =
            CreateRouter(CreateClient(_ =>
                new HttpResponseMessage(
                    HttpStatusCode.ServiceUnavailable)));

        DistributionRouteSummary? result =
            await router.GetSummaryAsync(
                [new DistributionRouteStopDto()],
                CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RoadRouter_ReturnsNull_WhenRouteIsMissing()
    {
        DistributionRoadRouter router =
            CreateRouter(CreateClient(_ =>
                JsonResponse(
                    """
                    {"routes":[]}
                    """)));

        DistributionRouteSummary? result =
            await router.GetSummaryAsync(
                [new DistributionRouteStopDto()],
                CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RoadRouter_ReturnsNull_OnInvalidJson()
    {
        DistributionRoadRouter router =
            CreateRouter(CreateClient(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{not-json",
                        Encoding.UTF8,
                        "application/json")
                }));

        DistributionRouteSummary? result =
            await router.GetSummaryAsync(
                [new DistributionRouteStopDto()],
                CancellationToken.None);

        Assert.Null(result);
    }

    private static DistributionRouteGeocoder CreateGeocoder(
        HttpClient client)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory
            .Setup(item => item.CreateClient(
                It.IsAny<string>()))
            .Returns(client);

        return new DistributionRouteGeocoder(
            factory.Object,
            Mock.Of<ILogger<DistributionRouteGeocoder>>());
    }

    private static DistributionRoadRouter CreateRouter(
        HttpClient client)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory
            .Setup(item => item.CreateClient(
                It.IsAny<string>()))
            .Returns(client);

        return new DistributionRoadRouter(
            factory.Object,
            Mock.Of<ILogger<DistributionRoadRouter>>());
    }

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> handler) =>
        new(new DelegateHandler(handler));

    private static HttpResponseMessage JsonResponse(
        string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json")
        };

    private static Order CreateOrder() =>
        new()
        {
            Address = "Русе",
            PostalCode = "7000",
            City = "Русе",
            Country = "България"
        };

    private sealed class DelegateHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
