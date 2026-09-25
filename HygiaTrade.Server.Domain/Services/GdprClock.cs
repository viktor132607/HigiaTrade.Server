namespace HygiaTrade.Domain.Services;

public interface IGdprClock
{
    DateTime UtcNow { get; }
}

public sealed class GdprClock : IGdprClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
