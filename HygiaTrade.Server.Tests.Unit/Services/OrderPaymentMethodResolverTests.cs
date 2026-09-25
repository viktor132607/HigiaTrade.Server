using HygiaTrade.Common.Options;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Domain.Services;
using Microsoft.Extensions.Options;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class OrderPaymentMethodResolverTests
{
    [Fact]
    public void Resolve_UsesFirstSupportedMethod_WhenMissing()
    {
        var resolver =
            new OrderPaymentMethodResolver(
                Options.Create(
                    new PaymentOptions
                    {
                        SupportedMethods =
                        [
                            "first",
                            "second"
                        ]
                    }));

        Assert.Equal("first", resolver.Resolve(null));
        Assert.Equal("first", resolver.Resolve(" "));
    }

    [Fact]
    public void Resolve_TrimsExplicitSupportedMethod()
    {
        var resolver =
            new OrderPaymentMethodResolver(
                Options.Create(
                    new PaymentOptions
                    {
                        SupportedMethods = ["CARD"]
                    }));

        Assert.Equal("card", resolver.Resolve(" card "));
    }

    [Fact]
    public void Resolve_Throws400_ForUnsupportedMethod()
    {
        var resolver =
            new OrderPaymentMethodResolver(
                Options.Create(new PaymentOptions()));

        AppException exception =
            Assert.Throws<AppException>(
                () => resolver.Resolve("cash"));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public void Resolve_Throws400_WhenNoMethodsConfigured()
    {
        var resolver =
            new OrderPaymentMethodResolver(
                Options.Create(
                    new PaymentOptions
                    {
                        SupportedMethods = []
                    }));

        AppException exception =
            Assert.Throws<AppException>(
                () => resolver.Resolve(null));

        Assert.Equal(400, exception.StatusCode);
    }
}
