namespace HygiaTrade.API.Services;

public interface INewProductsClock
{
    DateTime UtcNow { get; }
}

public sealed class NewProductsClock
    : INewProductsClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
