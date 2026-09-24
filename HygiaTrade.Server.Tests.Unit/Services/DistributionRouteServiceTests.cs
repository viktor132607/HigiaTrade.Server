using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using HygiaTrade.Core.Enums;
using HygiaTrade.Data.Entities;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class DistributionRouteServiceTests
{
    private readonly Mock<IDistributionRouteRepository> repository = new();
    private readonly Mock<IDistributionRouteGeocoder> geocoder = new();
    private readonly Mock<IDistributionRouteOptimizer> optimizer = new();
    private readonly Mock<IDistributionRoadRouter> roadRouter = new();
    private readonly Mock<IDistributionRouteDelay> delay = new();

    private DistributionRouteService CreateService() =>
        new(
            repository.Object,
            geocoder.Object,
            optimizer.Object,
            roadRouter.Object,
            delay.Object);

    [Fact]
    public async Task GetAsync_ReturnsSortedRoutesAndUnassignedActiveOrders()
    {
        Guid assignedId = Guid.NewGuid();
        Guid unassignedId = Guid.NewGuid();

        var older = new DistributionRouteDto
        {
            Id = Guid.NewGuid(),
            RouteDate = new DateOnly(2026, 9, 20),
            CreatedOn = new DateTime(2026, 9, 20),
            Stops =
            [
                new DistributionRouteStopDto
                {
                    OrderId = assignedId
                }
            ]
        };

        var newer = new DistributionRouteDto
        {
            Id = Guid.NewGuid(),
            RouteDate = new DateOnly(2026, 9, 21),
            CreatedOn = new DateTime(2026, 9, 21)
        };

        repository
            .Setup(item => item.GetStoreAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DistributionRouteStore
            {
                Routes = [older, newer]
            });

        repository
            .Setup(item => item.GetActiveOrdersAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CreateOrder(assignedId),
                CreateOrder(unassignedId)
            ]);

        DistributionRoutesPageResponse result =
            await CreateService().GetAsync(
                CancellationToken.None);

        Assert.Equal(newer.Id, result.Routes[0].Id);
        Assert.Equal(older.Id, result.Routes[1].Id);

        DistributionOrderDto order =
            Assert.Single(result.UnassignedOrders);

        Assert.Equal(unassignedId, order.Id);
        Assert.Equal("Русе, 7000, Русе, България", order.FullAddress);
        Assert.Equal(
            DistributionRouteMapping.DepotName,
            result.Depot.Name);
    }

    [Fact]
    public async Task OptimizeAsync_RejectsMissingDistributor()
    {
        var request = new CreateDistributionRouteRequest
        {
            DistributorName = " ",
            OrderIds = [Guid.NewGuid()]
        };

        DistributionRouteServiceException exception =
            await Assert.ThrowsAsync<DistributionRouteServiceException>(
                () => CreateService().OptimizeAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task OptimizeAsync_RejectsMissingOrders()
    {
        var request = new CreateDistributionRouteRequest
        {
            DistributorName = "Driver"
        };

        DistributionRouteServiceException exception =
            await Assert.ThrowsAsync<DistributionRouteServiceException>(
                () => CreateService().OptimizeAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task OptimizeAsync_RejectsMoreThan20Orders()
    {
        var request = new CreateDistributionRouteRequest
        {
            DistributorName = "Driver",
            OrderIds = Enumerable
                .Range(0, 21)
                .Select(_ => Guid.NewGuid())
                .ToList()
        };

        DistributionRouteServiceException exception =
            await Assert.ThrowsAsync<DistributionRouteServiceException>(
                () => CreateService().OptimizeAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task OptimizeAsync_RejectsAlreadyAssignedOrder()
    {
        Guid orderId = Guid.NewGuid();

        repository
            .Setup(item => item.GetStoreAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DistributionRouteStore
            {
                Routes =
                [
                    new DistributionRouteDto
                    {
                        Stops =
                        [
                            new DistributionRouteStopDto
                            {
                                OrderId = orderId
                            }
                        ]
                    }
                ]
            });

        var request = new CreateDistributionRouteRequest
        {
            DistributorName = "Driver",
            OrderIds = [orderId]
        };

        DistributionRouteServiceException exception =
            await Assert.ThrowsAsync<DistributionRouteServiceException>(
                () => CreateService().OptimizeAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task OptimizeAsync_RejectsOrdersThatNoLongerExist()
    {
        Guid orderId = Guid.NewGuid();
        SetupEmptyStore();

        repository
            .Setup(item => item.GetOrdersAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Order>());

        DistributionRouteServiceException exception =
            await Assert.ThrowsAsync<DistributionRouteServiceException>(
                () => CreateService().OptimizeAsync(
                    CreateRequest(orderId),
                    CancellationToken.None));

        Assert.Equal(400, exception.StatusCode);
    }

    [Theory]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task OptimizeAsync_RejectsTerminalOrders(
        OrderStatus status)
    {
        Guid orderId = Guid.NewGuid();
        SetupEmptyStore();

        repository
            .Setup(item => item.GetOrdersAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CreateOrder(orderId, status)
            ]);

        DistributionRouteServiceException exception =
            await Assert.ThrowsAsync<DistributionRouteServiceException>(
                () => CreateService().OptimizeAsync(
                    CreateRequest(orderId),
                    CancellationToken.None));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task OptimizeAsync_RejectsOrderWithoutAddress()
    {
        Guid orderId = Guid.NewGuid();
        SetupEmptyStore();

        repository
            .Setup(item => item.GetOrdersAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Order
                {
                    Id = orderId,
                    Status = OrderStatus.Processing
                }
            ]);

        DistributionRouteServiceException exception =
            await Assert.ThrowsAsync<DistributionRouteServiceException>(
                () => CreateService().OptimizeAsync(
                    CreateRequest(orderId),
                    CancellationToken.None));

        Assert.Equal(422, exception.StatusCode);
    }

    [Fact]
    public async Task OptimizeAsync_RejectsAddressThatCannotBeGeocoded()
    {
        Guid orderId = Guid.NewGuid();
        SetupEmptyStore();

        Order order = CreateOrder(orderId);

        repository
            .Setup(item => item.GetOrdersAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);

        geocoder
            .Setup(item => item.GeocodeOrderAsync(
                order,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DistributionGeoPoint?)null);

        DistributionRouteServiceException exception =
            await Assert.ThrowsAsync<DistributionRouteServiceException>(
                () => CreateService().OptimizeAsync(
                    CreateRequest(orderId),
                    CancellationToken.None));

        Assert.Equal(422, exception.StatusCode);
    }

    [Fact]
    public async Task OptimizeAsync_BuildsAndPersistsRoute()
    {
        Guid firstId = Guid.NewGuid();
        Guid secondId = Guid.NewGuid();

        SetupEmptyStore();

        Order first = CreateOrder(firstId);
        Order second = CreateOrder(secondId);

        repository
            .Setup(item => item.GetOrdersAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, second]);

        geocoder
            .Setup(item => item.GeocodeOrderAsync(
                first,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new DistributionGeoPoint(43.84, 25.96, "address"));

        geocoder
            .Setup(item => item.GeocodeOrderAsync(
                second,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new DistributionGeoPoint(43.85, 25.97, "city"));

        optimizer
            .Setup(item => item.Optimize(
                It.IsAny<IReadOnlyList<LocatedDistributionOrder>>()))
            .Returns((
                IReadOnlyList<LocatedDistributionOrder> items) =>
                items.Reverse().ToArray());

        optimizer
            .Setup(item => item.CalculateFallbackSummary(
                It.IsAny<IReadOnlyList<DistributionRouteStopDto>>()))
            .Returns(new DistributionRouteSummary(12.34, 61.4));

        roadRouter
            .Setup(item => item.GetSummaryAsync(
                It.IsAny<IReadOnlyList<DistributionRouteStopDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((DistributionRouteSummary?)null);

        delay
            .Setup(item => item.DelayAsync(
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        DateOnly routeDate = new(2026, 9, 25);

        var request = new CreateDistributionRouteRequest
        {
            DistributorName = "  Driver  ",
            RouteDate = routeDate,
            OrderIds = [firstId, firstId, secondId]
        };

        DistributionRouteDto result =
            await CreateService().OptimizeAsync(
                request,
                CancellationToken.None);

        Assert.Equal("Driver", result.DistributorName);
        Assert.Equal(routeDate, result.RouteDate);
        Assert.Equal(12.3, result.TotalDistanceKm);
        Assert.Equal(61, result.EstimatedDurationMinutes);
        Assert.Equal(secondId, result.Stops[0].OrderId);
        Assert.Equal(1, result.Stops[0].Position);
        Assert.Equal(firstId, result.Stops[1].OrderId);
        Assert.Equal(2, result.Stops[1].Position);
        Assert.Contains("google.com/maps/dir", result.NavigationUrl);

        delay.Verify(
            item => item.DelayAsync(
                TimeSpan.FromMilliseconds(1050),
                It.IsAny<CancellationToken>()),
            Times.Once);

        repository.Verify(
            item => item.SaveStoreAsync(
                It.Is<DistributionRouteStore>(
                    store =>
                        store.Routes.Count == 1 &&
                        store.Routes[0].Id == result.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OptimizeAsync_UsesRoadSummaryAndDefaultDate()
    {
        Guid orderId = Guid.NewGuid();
        SetupEmptyStore();

        Order order = CreateOrder(orderId);

        repository
            .Setup(item => item.GetOrdersAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([order]);

        var point =
            new DistributionGeoPoint(43.84, 25.96, "address");

        geocoder
            .Setup(item => item.GeocodeOrderAsync(
                order,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(point);

        optimizer
            .Setup(item => item.Optimize(
                It.IsAny<IReadOnlyList<LocatedDistributionOrder>>()))
            .Returns((
                IReadOnlyList<LocatedDistributionOrder> items) => items);

        roadRouter
            .Setup(item => item.GetSummaryAsync(
                It.IsAny<IReadOnlyList<DistributionRouteStopDto>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new DistributionRouteSummary(1.26, 0.2));

        repository
            .Setup(item => item.SaveStoreAsync(
                It.IsAny<DistributionRouteStore>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        DateOnly today =
            DateOnly.FromDateTime(DateTime.UtcNow);

        DistributionRouteDto result =
            await CreateService().OptimizeAsync(
                CreateRequest(orderId),
                CancellationToken.None);

        Assert.Equal(1.3, result.TotalDistanceKm);
        Assert.Equal(1, result.EstimatedDurationMinutes);
        Assert.Equal(today, result.RouteDate);
    }

    [Fact]
    public async Task DeleteAsync_RemovesAndPersistsRoute()
    {
        Guid routeId = Guid.NewGuid();

        repository
            .Setup(item => item.GetStoreAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DistributionRouteStore
            {
                Routes =
                [
                    new DistributionRouteDto
                    {
                        Id = routeId
                    }
                ]
            });

        repository
            .Setup(item => item.SaveStoreAsync(
                It.IsAny<DistributionRouteStore>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateService().DeleteAsync(
            routeId,
            CancellationToken.None);

        repository.Verify(
            item => item.SaveStoreAsync(
                It.Is<DistributionRouteStore>(
                    store => store.Routes.Count == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Throws404_WhenRouteIsMissing()
    {
        SetupEmptyStore();

        DistributionRouteServiceException exception =
            await Assert.ThrowsAsync<DistributionRouteServiceException>(
                () => CreateService().DeleteAsync(
                    Guid.NewGuid(),
                    CancellationToken.None));

        Assert.Equal(404, exception.StatusCode);
    }

    private void SetupEmptyStore() =>
        repository
            .Setup(item => item.GetStoreAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DistributionRouteStore());

    private static CreateDistributionRouteRequest CreateRequest(
        Guid orderId) =>
        new()
        {
            DistributorName = "Driver",
            OrderIds = [orderId]
        };

    private static Order CreateOrder(
        Guid id,
        OrderStatus status = OrderStatus.Processing) =>
        new()
        {
            Id = id,
            Names = "Customer",
            Phone = "0888123456",
            Address = "Русе",
            PostalCode = "7000",
            City = "Русе",
            Country = "България",
            Status = status,
            OrderTotalPrice = 42m
        };
}
