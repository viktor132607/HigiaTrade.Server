using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class ReportsRequestNormalizerTests
{
    private readonly ReportsRequestNormalizer normalizer = new();

    [Fact]
    public void Normalize_UsesDefaultThirtyDayWindow()
    {
        DateTime now =
            new(
                2026,
                9,
                25,
                12,
                30,
                0,
                DateTimeKind.Utc);

        ReportsRequest result =
            normalizer.Normalize(
                null,
                null,
                10,
                now);

        Assert.Equal(
            new DateOnly(2026, 8, 26),
            result.From);

        Assert.Equal(
            new DateOnly(2026, 9, 25),
            result.To);

        Assert.Equal(10, result.LowStockThreshold);

        Assert.Equal(
            new DateTime(
                2026,
                8,
                26,
                0,
                0,
                0,
                DateTimeKind.Utc),
            result.FromUtc);

        Assert.Equal(
            new DateTime(
                2026,
                9,
                26,
                0,
                0,
                0,
                DateTimeKind.Utc),
            result.ToExclusiveUtc);
    }

    [Fact]
    public void Normalize_UsesExplicitDates()
    {
        DateOnly from = new(2026, 1, 2);
        DateOnly to = new(2026, 1, 5);

        ReportsRequest result =
            normalizer.Normalize(
                from,
                to,
                5,
                DateTime.UtcNow);

        Assert.Equal(from, result.From);
        Assert.Equal(to, result.To);
    }

    [Fact]
    public void Normalize_RejectsReversedRange()
    {
        ReportsServiceException ex =
            Assert.Throws<ReportsServiceException>(
                () => normalizer.Normalize(
                    new DateOnly(2026, 2, 1),
                    new DateOnly(2026, 1, 1),
                    10,
                    DateTime.UtcNow));

        Assert.Equal(400, ex.StatusCode);

        Assert.Equal(
            "Началната дата не може да е след крайната дата.",
            ex.Message);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(15, 15)]
    [InlineData(1000001, 1000000)]
    public void Normalize_ClampsLowStockThreshold(
        int input,
        int expected)
    {
        ReportsRequest result =
            normalizer.Normalize(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 1),
                input,
                DateTime.UtcNow);

        Assert.Equal(
            expected,
            result.LowStockThreshold);
    }
}
