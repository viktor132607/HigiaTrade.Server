namespace HygiaTrade.Domain.Services;

public interface IBrandClock
{
    DateTime UtcNow { get; }
}

public sealed class BrandClock
    : IBrandClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
