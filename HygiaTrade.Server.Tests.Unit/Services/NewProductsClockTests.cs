using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class NewProductsClockTests
{
    [Fact]
    public void UtcNow_ReturnsUtcTimeCloseToSystemClock()
    {
        DateTime before = DateTime.UtcNow;

        DateTime actual =
            new NewProductsClock().UtcNow;

        DateTime after = DateTime.UtcNow;

        Assert.Equal(
            DateTimeKind.Utc,
            actual.Kind);

        Assert.InRange(
            actual,
            before,
            after);
    }
}
