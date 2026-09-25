using HygiaTrade.Common.Options;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Payments;
using Stripe;
using Stripe.Checkout;

namespace HygiaTrade.API.Services;

public sealed class StripeGateway(StripeOptions options) : IStripeGateway
{
    public bool Enabled => options.Enabled;
    public bool LiveMode => options.LiveMode;
    private SessionService Sessions => new(new StripeClient(options.SecretKey));

    public async Task<StripeSession> CreateAsync(Order order)
    {
        if (!Enabled) throw new AppException("Card payments are not configured.").SetStatusCode(503);
        if (!Uri.TryCreate(options.ClientUrl, UriKind.Absolute, out var origin)
            || (origin.Scheme != "https" && !(origin.Scheme == "http" && origin.IsLoopback)))
            throw new PaymentCreationRejectedException();
        if (order.StripeExpiresAt < DateTime.UtcNow.AddMinutes(31))
        {
            // Beyond the minimum creation window, discover a prior successful request before releasing stock.
            // This remains safe even after Stripe's idempotency cache expires.
            try {
                await foreach (var existing in Sessions.ListAutoPagingAsync(new SessionListOptions {
                    Limit = 100, Created = new DateRangeOptions { GreaterThanOrEqual = order.CreatedOn.AddMinutes(-1), LessThanOrEqual = order.StripeExpiresAt }
                }))
                    if (existing.ClientReferenceId == order.Id.ToString() && existing.Metadata?.GetValueOrDefault("application") == "higiatrade") return Map(existing);
                throw new PaymentCreationRejectedException();
            }
            catch (StripeException) { throw Unavailable(); }
            catch (HttpRequestException) { throw Unavailable(); }
        }
        string site = options.ClientUrl.TrimEnd('/');
        try
        {
            var session = await Sessions.CreateAsync(new SessionCreateOptions {
                Mode = "payment", PaymentMethodTypes = ["card"], CustomerEmail = order.GuestEmail,
                ClientReferenceId = order.Id.ToString(), Metadata = new() { ["order_id"] = order.Id.ToString(), ["application"] = "higiatrade" },
                SuccessUrl = site + "/checkout/stripe?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = site + "/checkout/stripe?cancelled=1&session_id={CHECKOUT_SESSION_ID}",
                ExpiresAt = order.StripeExpiresAt,
                LineItems = order.Items.OrderBy(i => i.ProductId).Select(item => new SessionLineItemOptions {
                    Quantity = item.Quantity,
                    PriceData = new SessionLineItemPriceDataOptions {
                        Currency = "eur", UnitAmount = decimal.ToInt64(item.SinglePrice * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = item.Title }
                    }
                }).ToList()
            }, new RequestOptions { IdempotencyKey = "higiatrade-checkout-" + order.Id });
            return Map(session);
        }
        catch (StripeException exception) when (exception.StripeError?.Type is "invalid_request_error" or "authentication_error")
        {
            // Stripe definitively rejected creation; no charge can exist for this request.
            throw new PaymentCreationRejectedException();
        }
        catch (StripeException) { throw Unavailable(); }
        catch (HttpRequestException) { throw Unavailable(); }
    }

    public async Task<StripeSession> GetAsync(string sessionId)
    {
        try { return Map(await Sessions.GetAsync(sessionId)); }
        catch (StripeException) { throw Unavailable(); }
        catch (HttpRequestException) { throw Unavailable(); }
    }
    public async Task ExpireAsync(string sessionId)
    {
        try { await Sessions.ExpireAsync(sessionId); }
        catch (StripeException) {
            // Payment may have completed while the customer clicked cancel.
            if ((await GetAsync(sessionId)).Status == "open") throw Unavailable();
        }
    }
    public string? ReadWebhookSession(string payload, string signature)
    {
        if (string.IsNullOrWhiteSpace(options.WebhookSecret)) throw new AppException("Webhook is not configured.").SetStatusCode(503);
        try {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, options.WebhookSecret, throwOnApiVersionMismatch: false);
            if (stripeEvent.Livemode != LiveMode) throw new AppException("Wrong payment mode.").SetStatusCode(400);
            return stripeEvent.Type is "checkout.session.completed" or "checkout.session.async_payment_succeeded"
                or "checkout.session.expired" or "checkout.session.async_payment_failed"
                ? (stripeEvent.Data.Object as Session)?.Id : null;
        }
        catch (StripeException) { throw new AppException("Invalid webhook signature.").SetStatusCode(400); }
    }
    private static StripeSession Map(Session session) => new(session.Id, session.Url, session.Status, session.PaymentStatus,
        session.Metadata?.GetValueOrDefault("order_id") ?? "", session.AmountTotal ?? 0, session.Currency, session.Livemode);
    private static AppException Unavailable() => new AppException("Payment provider unavailable. Retry the same payment.").SetStatusCode(503);
}
