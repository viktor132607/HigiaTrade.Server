using HygiaTrade.Domain.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class GdprClockTests
{
    [Fact]
    public void UtcNow_ReturnsUtcTimeCloseToSystemClock()
    {
        DateTime before = DateTime.UtcNow;

        DateTime actual =
            new GdprClock().UtcNow;

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
