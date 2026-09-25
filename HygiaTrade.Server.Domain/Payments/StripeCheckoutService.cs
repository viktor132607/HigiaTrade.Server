using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.Domain.Payments;

public record StripeCheckoutResult(string? SessionId, string? Url, Guid OrderId, string PaymentStatus, decimal Total, string Currency = "eur");

public sealed class StripeCheckoutService(IStripeOrderRepository repository, IOrderPricingService pricing, IStripeGateway gateway)
{
    public async Task<StripeCheckoutResult> StartAsync(StripeCheckoutRequest request, Guid? userId)
    {
        if (!gateway.Enabled) throw new AppException("Card payments are not configured.").SetStatusCode(503);
        if (!request.ConsentAccepted || request.CheckoutAttemptId == Guid.Empty || request.Items.Count is < 1 or > 100
            || request.Items.Any(i => i.Quantity < 1 || i.Quantity > 100000))
            throw new AppException("Invalid checkout details or consent.").SetStatusCode(400);
        if (request.PaymentMethod != "online-card") throw new AppException("Unsupported payment method.").SetStatusCode(400);
        CheckoutPolicy.Validate(request, allowCard: true);
        string fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { request, userId }))));
        var order = await repository.LockedAsync(request.CheckoutAttemptId, async () =>
        {
            var existing = await repository.FindAttemptAsync(request.CheckoutAttemptId);
            if (existing is not null)
            {
                if (existing.StripeCheckoutFingerprint != fingerprint) throw new AppException("Checkout changed. Start a new payment.").SetStatusCode(409);
                return existing;
            }
            var created = new Order {
                UserId = userId, GuestEmail = request.Email.Trim(), Names = request.Names.Trim(), Phone = request.Phone.Trim(),
                Country = request.Country.Trim(), City = request.City.Trim(), PostalCode = request.PostalCode.Trim(), Address = request.Address.Trim(),
                Status = OrderStatus.AwaitingPayment, PaymentStatus = "Pending", StripeCheckoutAttemptId = request.CheckoutAttemptId,
                StripeCheckoutFingerprint = fingerprint, StripeLiveMode = gateway.LiveMode, StripeExpiresAt = DateTime.UtcNow.AddHours(1)
            };
            CheckoutPolicy.Apply(created, request);
            foreach (var group in request.Items.GroupBy(i => i.ProductId).OrderBy(g => g.Key))
            {
                int quantity = checked(group.Sum(i => i.Quantity));
                var product = await repository.LockProductAsync(group.Key);
                if (product is null || !product.IsActive || product.IsDeleted || product.Quantity < quantity)
                    throw new AppException("Insufficient stock for the selected products.").SetStatusCode(409);
                var item = new OrderItem { ProductId = product.Id, Title = product.Title, PrimaryImageUri = product.MainImageUrl, Quantity = quantity, SinglePrice = 0m, TotalPrice = 0m };
                pricing.ApplyCurrentPricing(item, product, quantity);
                created.Items.Add(item);
                product.Quantity -= (uint)quantity;
            }
            pricing.UpdateOrderPrices(created);
            CheckoutPolicy.EnsureMinimum(created.OrderTotalPrice);
            if (created.OrderTotalPrice != request.ExpectedTotal)
                throw new AppException("Prices changed. Review the cart before payment.").SetStatusCode(409);
            await repository.AddAsync(created);
            return created;
        });
        return await EnsureSessionAsync(order);
    }

    public async Task<StripeCheckoutResult> EnsureSessionAsync(Order order)
    {
        if (order.StripeSessionId is not null) return await RefreshAsync(order.StripeSessionId);
        if (order.PaymentStatus != "Pending") throw new AppException("Payment session expired. Start a new payment.").SetStatusCode(400);
        if (order.StripeLiveMode != gateway.LiveMode) throw new AppException("Payment mode changed. Contact support.").SetStatusCode(503);
        StripeSession session;
        try { session = await gateway.CreateAsync(order); }
        catch (PaymentCreationRejectedException) {
            await repository.LockedAsync(order.StripeCheckoutAttemptId!.Value, async () => {
                var current = (await repository.FindAttemptAsync(order.StripeCheckoutAttemptId.Value))!;
                if (current.StripeSessionId is null) await repository.FinalizeAsync(current, false);
                return true;
            });
            throw new AppException("Payment configuration rejected. Start a new payment after checking configuration.").SetStatusCode(503);
        }
        ValidateSession(order, session);
        return await repository.LockedAsync(order.StripeCheckoutAttemptId!.Value, async () =>
        {
            var current = (await repository.FindAttemptAsync(order.StripeCheckoutAttemptId.Value))!;
            current.StripeSessionId = session.Id;
            current.StripeCheckoutUrl = session.Url;
            current.ModifiedOn = DateTime.UtcNow;
            await repository.SaveAsync();
            return Result(current);
        });
    }

    public async Task<StripeCheckoutResult> RefreshAsync(string sessionId)
    {
        var order = await repository.FindSessionAsync(sessionId) ?? throw new AppException("Payment session not found.").SetStatusCode(404);
        var session = await gateway.GetAsync(sessionId);
        ValidateSession(order, session);
        return await repository.LockedAsync(order.StripeCheckoutAttemptId!.Value, async () =>
        {
            var current = (await repository.FindSessionAsync(sessionId))!;
            if (session.Status == "complete" && session.PaymentStatus == "paid") await repository.FinalizeAsync(current, true);
            else if (session.Status == "expired") await repository.FinalizeAsync(current, false);
            current.ModifiedOn = DateTime.UtcNow;
            await repository.SaveAsync();
            return Result(current);
        });
    }

    public async Task<StripeCheckoutResult> ResumeAttemptAsync(Guid attemptId)
    {
        var order = await repository.FindAttemptAsync(attemptId) ?? throw new AppException("Payment session not found.").SetStatusCode(404);
        return order.PaymentStatus == "Pending" ? await EnsureSessionAsync(order) : Result(order);
    }

    public async Task<StripeCheckoutResult> CancelAsync(string sessionId)
    {
        var order = await repository.FindSessionAsync(sessionId) ?? throw new AppException("Payment session not found.").SetStatusCode(404);
        var session = await gateway.GetAsync(sessionId);
        ValidateSession(order, session);
        if (session.Status == "open") await gateway.ExpireAsync(sessionId);
        return await RefreshAsync(sessionId);
    }

    public async Task HandleWebhookAsync(string payload, string signature)
    {
        string? id = gateway.ReadWebhookSession(payload, signature);
        // A shared Stripe account can send events belonging to other projects.
        if (id is not null && await repository.FindSessionAsync(id) is not null) await RefreshAsync(id);
    }

    public async Task ReconcileAsync()
    {
        if (!gateway.Enabled) return;
        foreach (var order in await repository.PendingAsync())
        {
            if (order.StripeLiveMode != gateway.LiveMode) continue;
            try
            {
                if (order.StripeSessionId is not null) await RefreshAsync(order.StripeSessionId);
                else await EnsureSessionAsync(order);
            }
            catch (AppException) { /* Leave the reservation pending on ambiguous provider failures. Retry next pass. */ }
        }
    }

    public static void ValidateSession(Order order, StripeSession session)
    {
        if (session.OrderId != order.Id.ToString() || session.AmountTotal != decimal.ToInt64(order.OrderTotalPrice * 100)
            || session.Currency != "eur" || session.LiveMode != order.StripeLiveMode)
            throw new AppException("Payment verification failed.").SetStatusCode(409);
    }
    private static StripeCheckoutResult Result(Order order) => new(order.StripeSessionId!,
        order.PaymentStatus == "Pending" ? order.StripeCheckoutUrl : null, order.Id, order.PaymentStatus, order.OrderTotalPrice);
}
