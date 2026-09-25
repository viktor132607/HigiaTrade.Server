using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Payments;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public class StripeCheckoutServiceTests
{
    private readonly Mock<IStripeOrderRepository> repository = new();
    private readonly Mock<IStripeGateway> gateway = new();
    private readonly Product product = new() { Id = Guid.NewGuid(), Title = "Soap", MainImageUrl = "p", Description = "", RegularPrice = 25, Quantity = 10 };
    private Order? saved;
    private StripeSession? session;
    private readonly StripeCheckoutService service;

    public StripeCheckoutServiceTests()
    {
        repository.Setup(r => r.LockedAsync(It.IsAny<Guid>(), It.IsAny<Func<Task<Order>>>())).Returns<Guid, Func<Task<Order>>>((_, f) => f());
        repository.Setup(r => r.LockedAsync(It.IsAny<Guid>(), It.IsAny<Func<Task<StripeCheckoutResult>>>())).Returns<Guid, Func<Task<StripeCheckoutResult>>>((_, f) => f());
        repository.Setup(r => r.LockedAsync(It.IsAny<Guid>(), It.IsAny<Func<Task<bool>>>())).Returns<Guid, Func<Task<bool>>>((_, f) => f());
        repository.Setup(r => r.FindAttemptAsync(It.IsAny<Guid>())).ReturnsAsync(() => saved);
        repository.Setup(r => r.FindSessionAsync(It.IsAny<string>())).ReturnsAsync(() => saved);
        repository.Setup(r => r.LockProductAsync(product.Id)).ReturnsAsync(product);
        repository.Setup(r => r.AddAsync(It.IsAny<Order>())).Callback<Order>(o => saved = o).Returns(Task.CompletedTask);
        repository.Setup(r => r.SaveAsync()).Returns(Task.CompletedTask);
        repository.Setup(r => r.FinalizeAsync(It.IsAny<Order>(), It.IsAny<bool>())).Callback<Order, bool>((o, paid) => {
            if (o.PaymentStatus != "Pending") return;
            o.PaymentStatus = paid ? "Paid" : "Expired";
            o.Status = paid ? OrderStatus.PendingVerification : OrderStatus.Cancelled;
            if (!paid) product.Quantity += (uint)o.Items.Sum(i => i.Quantity);
        }).Returns(Task.CompletedTask);
        gateway.SetupGet(g => g.Enabled).Returns(true);
        gateway.Setup(g => g.CreateAsync(It.IsAny<Order>())).ReturnsAsync((Order o) => session = new("cs_test_example", "https://checkout.stripe.com/c/pay/test", "open", "unpaid", o.Id.ToString(), 5000, "eur", false));
        gateway.Setup(g => g.GetAsync(It.IsAny<string>())).ReturnsAsync(() => session!);
        service = new(repository.Object, new OrderPricingService(Mock.Of<IProductRepository>()), gateway.Object);
    }
    private StripeCheckoutRequest Request() => new() {
        CheckoutAttemptId = Guid.NewGuid(), ExpectedTotal = 50, Names = "Customer", Email = "a@example.com",
        Phone = "+359888123456", Country = "BG", City = "Ruse", PostalCode = "7000", Address = "Street 1",
        PaymentMethod = "online-card", DeliveryMethod = "regional-delivery", ConsentAccepted = true,
        Items = [new() { ProductId = product.Id, Quantity = 2 }]
    };
    [Fact]
    public async Task CreatesPricedOrderAndReservesStockOnceAcrossRetry()
    {
        var request = Request();
        var first = await service.StartAsync(request, null);
        var second = await service.StartAsync(request, null);
        Assert.Equal(first.SessionId, second.SessionId);
        Assert.Equal((uint)8, product.Quantity);
        Assert.Equal(50m, saved!.OrderTotalPrice);
        Assert.Equal(OrderStatus.AwaitingPayment, saved.Status);
        repository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        gateway.Verify(g => g.CreateAsync(It.IsAny<Order>()), Times.Once);
    }
    [Fact]
    public async Task RejectsChangedPayloadOnSameAttempt()
    {
        var request = Request(); await service.StartAsync(request, null); request.Address = "Different";
        await Assert.ThrowsAsync<AppException>(() => service.StartAsync(request, null));
        Assert.Equal((uint)8, product.Quantity);
    }
    [Fact]
    public async Task RejectsClientAmountTamperingBeforeCreatingSession()
    {
        var request = Request(); request.ExpectedTotal = 1;
        await Assert.ThrowsAsync<AppException>(() => service.StartAsync(request, null));
        gateway.Verify(g => g.CreateAsync(It.IsAny<Order>()), Times.Never);
        repository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }
    [Fact]
    public async Task RejectsInsufficientStock()
    {
        product.Quantity = 1;
        await Assert.ThrowsAsync<AppException>(() => service.StartAsync(Request(), null));
        gateway.Verify(g => g.CreateAsync(It.IsAny<Order>()), Times.Never);
    }
    [Fact]
    public async Task UnpaidSessionNeverConfirmsOrderAndPaidSessionDoes()
    {
        var result = await service.StartAsync(Request(), null);
        Assert.Equal("Pending", (await service.RefreshAsync(result.SessionId!)).PaymentStatus);
        session = session! with { Status = "complete", PaymentStatus = "paid" };
        Assert.Equal("Paid", (await service.RefreshAsync(result.SessionId!)).PaymentStatus);
        await service.RefreshAsync(result.SessionId!);
        Assert.Equal((uint)8, product.Quantity);
        Assert.Equal(OrderStatus.PendingVerification, saved!.Status);
    }
    [Fact]
    public async Task ExpiryRestoresStockOnceAcrossDuplicateEvents()
    {
        var result = await service.StartAsync(Request(), null);
        session = session! with { Status = "expired" };
        await service.RefreshAsync(result.SessionId!); await service.RefreshAsync(result.SessionId!);
        Assert.Equal((uint)10, product.Quantity); Assert.Equal("Expired", saved!.PaymentStatus);
    }
    [Fact]
    public async Task DefiniteCreationRejectionReleasesReservation()
    {
        gateway.Setup(g => g.CreateAsync(It.IsAny<Order>())).ThrowsAsync(new PaymentCreationRejectedException());
        await Assert.ThrowsAsync<AppException>(() => service.StartAsync(Request(), null));
        Assert.Equal((uint)10, product.Quantity); Assert.Equal("Expired", saved!.PaymentStatus);
    }
    [Fact]
    public async Task AmbiguousProviderFailureKeepsReservationForReconciliation()
    {
        gateway.Setup(g => g.CreateAsync(It.IsAny<Order>())).ThrowsAsync(new AppException("Unavailable"));
        await Assert.ThrowsAsync<AppException>(() => service.StartAsync(Request(), null));
        Assert.Equal((uint)8, product.Quantity); Assert.Equal("Pending", saved!.PaymentStatus);
    }
    [Theory]
    [InlineData(4999, "eur", false, false)]
    [InlineData(5000, "usd", false, false)]
    [InlineData(5000, "eur", true, false)]
    [InlineData(5000, "eur", false, true)]
    public async Task RejectsMismatchedAmountCurrencyModeOrOrder(long amount, string currency, bool live, bool otherOrder)
    {
        var result = await service.StartAsync(Request(), null);
        session = session! with { AmountTotal = amount, Currency = currency, LiveMode = live, OrderId = otherOrder ? Guid.NewGuid().ToString() : saved!.Id.ToString(), Status = "complete", PaymentStatus = "paid" };
        await Assert.ThrowsAsync<AppException>(() => service.RefreshAsync(result.SessionId!));
        Assert.Equal("Pending", saved!.PaymentStatus);
    }
    [Fact]
    public async Task DoesNotCancelPaymentThatCompletedDuringRedirect()
    {
        var result = await service.StartAsync(Request(), null);
        session = session! with { Status = "complete", PaymentStatus = "paid" };
        Assert.Equal("Paid", (await service.CancelAsync(result.SessionId!)).PaymentStatus);
        gateway.Verify(g => g.ExpireAsync(It.IsAny<string>()), Times.Never);
    }
}
