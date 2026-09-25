using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;
namespace HygiaTrade.Tests.Unit.Services;

public class CheckoutPolicyTests
{
    private static SendOrderRequest Request() => new() {
        Names = "Customer", PostalCode = "7000", Country = "BG", City = "Ruse",
        Address = "Street 1", Phone = "+359888123456", DeliveryMethod = "regional-delivery",
        PaymentMethod = "cash-on-delivery"
    };
    [Theory]
    [InlineData("Русе")][InlineData("Силистра")][InlineData("Разград")]
    [InlineData("Свищов")][InlineData("Бяла")][InlineData("Търговище")]
    public void AcceptsServedRegionsWithSeparateSettlement(string region) {
        var request = Request(); request.DeliveryRegion = region; request.City = "Village";
        CheckoutPolicy.Validate(request);
    }
    [Theory]
    [InlineData("Sofia", "BG")][InlineData("Ruse", "Romania")]
    public void RejectsUnsupportedAddress(string city, string country) {
        var request = Request(); request.City = city; request.Country = country;
        Assert.Throws<AppException>(() => CheckoutPolicy.Validate(request));
    }
    [Theory]
    [InlineData("online-card")][InlineData("fake")][InlineData(null)]
    public void RejectsUnavailablePayments(string? method) {
        var request = Request(); request.PaymentMethod = method;
        Assert.Throws<AppException>(() => CheckoutPolicy.Validate(request));
    }
    [Fact]
    public void RejectsIncompleteInvoice() {
        var request = Request(); request.InvoiceRequested = true;
        Assert.Throws<AppException>(() => CheckoutPolicy.Validate(request));
    }
    [Fact]
    public void PersistsInvoiceAndClearsItWhenUnchecked() {
        var request = Request(); request.InvoiceRequested = true;
        request.InvoiceCompanyName = " Company "; request.InvoiceCompanyId = "123456789";
        request.InvoiceAddress = " Ruse "; request.InvoiceVatId = "BG123456789";
        CheckoutPolicy.Validate(request);
        var order = new Order(); CheckoutPolicy.Apply(order, request);
        Assert.True(order.InvoiceRequested); Assert.Equal("Company", order.InvoiceCompanyName);
        Assert.Equal("cash-on-delivery", order.PaymentMethod);
        var response = OrderMapping.ToResponse(order); Assert.Equal("123456789", response.InvoiceCompanyId);
        request.InvoiceRequested = false; CheckoutPolicy.Apply(order, request);
        Assert.False(order.InvoiceRequested); Assert.Null(order.InvoiceCompanyName); Assert.Null(order.InvoiceCompanyId);
    }
    [Theory]
    [InlineData(0)][InlineData(49.99)]
    public void RejectsBelowMinimum(decimal amount) => Assert.Throws<AppException>(() => CheckoutPolicy.EnsureMinimum(amount));
    [Theory]
    [InlineData(50)][InlineData(50.01)]
    public void AcceptsMinimum(decimal amount) => CheckoutPolicy.EnsureMinimum(amount);
}
