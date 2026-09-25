using HygiaTrade.Data.Entities;
namespace HygiaTrade.Domain.Payments;

public record StripeSession(string Id, string? Url, string Status, string PaymentStatus,
    string OrderId, long AmountTotal, string Currency, bool LiveMode);

public interface IStripeGateway
{
    bool Enabled { get; }
    bool LiveMode { get; }
    Task<StripeSession> CreateAsync(Order order);
    Task<StripeSession> GetAsync(string sessionId);
    Task ExpireAsync(string sessionId);
    string? ReadWebhookSession(string payload, string signature);
}

public sealed class PaymentCreationRejectedException : Exception { }
