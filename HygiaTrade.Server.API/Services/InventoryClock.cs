namespace HygiaTrade.API.Services;

public interface IInventoryClock
{
    DateTime UtcNow { get; }
}

public sealed class InventoryClock
    : IInventoryClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
