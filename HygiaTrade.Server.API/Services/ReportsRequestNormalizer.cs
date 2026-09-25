namespace HygiaTrade.API.Services;

public sealed record ReportsRequest(
    DateOnly From,
    DateOnly To,
    int LowStockThreshold,
    DateTime FromUtc,
    DateTime ToExclusiveUtc);

public interface IReportsRequestNormalizer
{
    ReportsRequest Normalize(
        DateOnly? from,
        DateOnly? to,
        int lowStockThreshold,
        DateTime utcNow);
}

public sealed class ReportsRequestNormalizer
    : IReportsRequestNormalizer
{
    public ReportsRequest Normalize(
        DateOnly? from,
        DateOnly? to,
        int lowStockThreshold,
        DateTime utcNow)
    {
        DateOnly today =
            DateOnly.FromDateTime(utcNow);

        DateOnly fromDate =
            from ?? today.AddDays(-30);

        DateOnly toDate =
            to ?? today;

        if (fromDate > toDate)
        {
            throw new ReportsServiceException(
                StatusCodes.Status400BadRequest,
                "Началната дата не може да е след крайната дата.");
        }

        int normalizedThreshold =
            Math.Clamp(
                lowStockThreshold,
                0,
                1_000_000);

        DateTime fromUtc =
            DateTime.SpecifyKind(
                fromDate.ToDateTime(
                    TimeOnly.MinValue),
                DateTimeKind.Utc);

        DateTime toExclusiveUtc =
            DateTime.SpecifyKind(
                toDate
                    .AddDays(1)
                    .ToDateTime(
                        TimeOnly.MinValue),
                DateTimeKind.Utc);

        return new ReportsRequest(
            fromDate,
            toDate,
            normalizedThreshold,
            fromUtc,
            toExclusiveUtc);
    }
}
