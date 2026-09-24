using HygiaTrade.API.Controllers;
using HygiaTrade.Core.Enums;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.API.Services;

public interface IDistributionRouteService
{
    Task<DistributionRoutesPageResponse> GetAsync(
        CancellationToken cancellationToken);

    Task<DistributionRouteDto> OptimizeAsync(
        CreateDistributionRouteRequest request,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        Guid routeId,
        CancellationToken cancellationToken);
}

public interface IDistributionRouteDelay
{
    Task DelayAsync(
        TimeSpan delay,
        CancellationToken cancellationToken);
}

public sealed class DistributionRouteDelay : IDistributionRouteDelay
{
    public Task DelayAsync(
        TimeSpan delay,
        CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);
}

public sealed class DistributionRouteServiceException(
    int statusCode,
    object payload) : Exception
{
    public int StatusCode { get; } = statusCode;
    public object Payload { get; } = payload;
}

public sealed class DistributionRouteService(
    IDistributionRouteRepository repository,
    IDistributionRouteGeocoder geocoder,
    IDistributionRouteOptimizer optimizer,
    IDistributionRoadRouter roadRouter,
    IDistributionRouteDelay delay) : IDistributionRouteService
{
    private const int MaxOrdersPerRoute = 20;

    public async Task<DistributionRoutesPageResponse> GetAsync(
        CancellationToken cancellationToken)
    {
        DistributionRouteStore store =
            await repository.GetStoreAsync(cancellationToken);

        HashSet<Guid> assignedOrderIds = store.Routes
            .SelectMany(route => route.Stops)
            .Select(stop => stop.OrderId)
            .ToHashSet();

        IReadOnlyList<Order> orders =
            await repository.GetActiveOrdersAsync(cancellationToken);

        List<DistributionOrderDto> unassignedOrders = orders
            .Where(order => !assignedOrderIds.Contains(order.Id))
            .Select(DistributionRouteMapping.ToOrderDto)
            .ToList();

        return new DistributionRoutesPageResponse
        {
            Depot = DistributionRouteMapping.CreateDepot(),
            Routes = store.Routes
                .OrderByDescending(route => route.RouteDate)
                .ThenByDescending(route => route.CreatedOn)
                .ToList(),
            UnassignedOrders = unassignedOrders
        };
    }

    public async Task<DistributionRouteDto> OptimizeAsync(
        CreateDistributionRouteRequest request,
        CancellationToken cancellationToken)
    {
        string distributorName =
            (request.DistributorName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(distributorName))
        {
            throw new DistributionRouteServiceException(
                400,
                new { message = "Въведи име на дистрибутор." });
        }

        List<Guid> requestedIds = (request.OrderIds ?? [])
            .Distinct()
            .ToList();

        if (requestedIds.Count == 0)
        {
            throw new DistributionRouteServiceException(
                400,
                new { message = "Избери поне една поръчка за маршрута." });
        }

        if (requestedIds.Count > MaxOrdersPerRoute)
        {
            throw new DistributionRouteServiceException(
                400,
                new
                {
                    message =
                        $"Един маршрут може да съдържа най-много {MaxOrdersPerRoute} поръчки."
                });
        }

        DistributionRouteStore store =
            await repository.GetStoreAsync(cancellationToken);

        HashSet<Guid> assignedOrderIds = store.Routes
            .SelectMany(route => route.Stops)
            .Select(stop => stop.OrderId)
            .ToHashSet();

        List<Guid> alreadyAssigned = requestedIds
            .Where(assignedOrderIds.Contains)
            .ToList();

        if (alreadyAssigned.Count > 0)
        {
            throw new DistributionRouteServiceException(
                409,
                new
                {
                    message =
                        "Някои от избраните поръчки вече са включени в друг маршрут.",
                    orderIds = alreadyAssigned
                });
        }

        IReadOnlyList<Order> orders =
            await repository.GetOrdersAsync(
                requestedIds,
                cancellationToken);

        if (orders.Count != requestedIds.Count)
        {
            throw new DistributionRouteServiceException(
                400,
                new
                {
                    message =
                        "Една или повече поръчки вече не съществуват."
                });
        }

        if (orders.Any(order =>
                order.Status is
                    OrderStatus.Delivered or
                    OrderStatus.Cancelled))
        {
            throw new DistributionRouteServiceException(
                400,
                new
                {
                    message =
                        "Доставена или отказана поръчка не може да бъде добавена към маршрут."
                });
        }

        List<LocatedDistributionOrder> locatedOrders = [];

        for (int index = 0; index < orders.Count; index++)
        {
            Order order = orders[index];

            string fullAddress =
                DistributionRouteMapping.BuildOrderAddress(order);

            if (string.IsNullOrWhiteSpace(fullAddress))
            {
                throw new DistributionRouteServiceException(
                    422,
                    new
                    {
                        message =
                            "Поръчката няма достатъчно адресни данни за маршрут.",
                        orderId = order.Id
                    });
            }

            DistributionGeoPoint? point =
                await geocoder.GeocodeOrderAsync(
                    order,
                    cancellationToken);

            if (point is null)
            {
                throw new DistributionRouteServiceException(
                    422,
                    new
                    {
                        message =
                            "Адресът на поръчката не можа да бъде намерен на картата.",
                        orderId = order.Id,
                        address = fullAddress
                    });
            }

            locatedOrders.Add(
                new LocatedDistributionOrder(order, point));

            if (index < orders.Count - 1)
            {
                await delay.DelayAsync(
                    TimeSpan.FromMilliseconds(1050),
                    cancellationToken);
            }
        }

        IReadOnlyList<LocatedDistributionOrder> optimized =
            optimizer.Optimize(locatedOrders);

        List<DistributionRouteStopDto> stops = optimized
            .Select((item, index) =>
                DistributionRouteMapping.ToStopDto(
                    item.Order,
                    item.Point,
                    index + 1))
            .ToList();

        DistributionRouteSummary summary =
            await roadRouter.GetSummaryAsync(
                stops,
                cancellationToken)
            ?? optimizer.CalculateFallbackSummary(stops);

        DateOnly routeDate =
            request.RouteDate ??
            DateOnly.FromDateTime(DateTime.UtcNow);

        var route = new DistributionRouteDto
        {
            Id = Guid.NewGuid(),
            DistributorName = distributorName,
            RouteDate = routeDate,
            CreatedOn = DateTime.UtcNow,
            Start = DistributionRouteMapping.CreateDepot(),
            End = DistributionRouteMapping.CreateDepot(),
            TotalDistanceKm = Math.Round(summary.DistanceKm, 1),
            EstimatedDurationMinutes = Math.Max(
                1,
                (int)Math.Round(summary.DurationMinutes)),
            NavigationUrl =
                DistributionRouteMapping.BuildGoogleMapsUrl(stops),
            Stops = stops
        };

        store.Routes.Add(route);
        await repository.SaveStoreAsync(store, cancellationToken);

        return route;
    }

    public async Task DeleteAsync(
        Guid routeId,
        CancellationToken cancellationToken)
    {
        DistributionRouteStore store =
            await repository.GetStoreAsync(cancellationToken);

        int removed =
            store.Routes.RemoveAll(route => route.Id == routeId);

        if (removed == 0)
        {
            throw new DistributionRouteServiceException(
                404,
                new { message = "Маршрутът не е намерен." });
        }

        await repository.SaveStoreAsync(store, cancellationToken);
    }
}
