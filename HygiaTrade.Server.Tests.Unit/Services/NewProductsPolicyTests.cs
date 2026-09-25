using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class NewProductsPolicyTests
{
    private readonly NewProductsPolicy policy = new();

    [Theory]
    [InlineData(-5, 0, 1, 1)]
    [InlineData(1, 1, 1, 1)]
    [InlineData(2, 50, 2, 50)]
    [InlineData(3, 999, 3, 200)]
    public void NormalizePage_ClampsValues(
        int pageNumber,
        int pageSize,
        int expectedPage,
        int expectedSize)
    {
        NewProductsPage result =
            policy.NormalizePage(
                pageNumber,
                pageSize);

        Assert.Equal(
            expectedPage,
            result.PageNumber);

        Assert.Equal(
            expectedSize,
            result.PageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(14)]
    [InlineData(365)]
    public void ValidateDisplayDays_AcceptsSupportedRange(
        int displayDays)
    {
        policy.ValidateDisplayDays(displayDays);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    public void ValidateDisplayDays_RejectsUnsupportedRange(
        int displayDays)
    {
        NewProductsServiceException ex =
            Assert.Throws<NewProductsServiceException>(
                () => policy.ValidateDisplayDays(
                    displayDays));

        Assert.Equal(400, ex.StatusCode);

        Assert.Equal(
            "DisplayDays must be between 1 and 365.",
            ex.Message);
    }

    [Fact]
    public void CreateStatus_ReturnsInactiveDefault_WhenNoRecordExists()
    {
        NewProductStatusDto result =
            policy.CreateStatus(
                null,
                DateTime.UtcNow);

        Assert.False(result.IsNewProduct);
        Assert.Equal(14, result.DisplayDays);
        Assert.Null(result.ActiveUntilUtc);
        Assert.False(result.IsCurrentlyNew);
    }

    [Fact]
    public void CreateStatus_ReturnsCurrentlyNew_WhenActiveUntilIsFuture()
    {
        DateTime now =
            new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var record =
            new NewProductStatusRecord(
                30,
                now.AddDays(1));

        NewProductStatusDto result =
            policy.CreateStatus(record, now);

        Assert.True(result.IsNewProduct);
        Assert.Equal(30, result.DisplayDays);
        Assert.Equal(
            record.ActiveUntilUtc,
            result.ActiveUntilUtc);
        Assert.True(result.IsCurrentlyNew);
    }

    [Fact]
    public void CreateStatus_ReturnsExpiredConfiguredStatus_WhenActiveUntilIsNotFuture()
    {
        DateTime now =
            new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var record =
            new NewProductStatusRecord(
                7,
                now);

        NewProductStatusDto result =
            policy.CreateStatus(record, now);

        Assert.True(result.IsNewProduct);
        Assert.Equal(7, result.DisplayDays);
        Assert.Equal(now, result.ActiveUntilUtc);
        Assert.False(result.IsCurrentlyNew);
    }

    [Fact]
    public void CreateInactiveStatus_ReturnsStableDefault()
    {
        NewProductStatusDto result =
            policy.CreateInactiveStatus();

        Assert.False(result.IsNewProduct);
        Assert.Equal(14, result.DisplayDays);
        Assert.Null(result.ActiveUntilUtc);
        Assert.False(result.IsCurrentlyNew);
    }
}
