using System.Security.Claims;
using HygiaTrade.API.Helpers;
using HygiaTrade.Common.Options;
using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Domain.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/payments/stripe")]
public sealed class PaymentsController(StripeCheckoutService checkout, StripeOptions options) : ControllerBase
{
    [AllowAnonymous, HttpGet("config")]
    public IActionResult Config() => Ok(new { enabled = options.Enabled, testMode = !options.LiveMode });

    [AllowAnonymous, HttpPost("checkout"), EnableRateLimiting("stripe-checkout")]
    public Task<IActionResult> Create(StripeCheckoutRequest request) => ControllerProcessor.ProcessAsync(() =>
        checkout.StartAsync(request, Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null), this, true);

    // The unguessable Stripe session ID is a receipt capability. Never return customer PII here.
    [AllowAnonymous, HttpGet("status"), EnableRateLimiting("stripe-status")]
    public Task<IActionResult> Status([FromQuery] string sessionId) => ControllerProcessor.ProcessAsync(() => checkout.RefreshAsync(sessionId), this);

    [AllowAnonymous, HttpGet("attempt"), EnableRateLimiting("stripe-status")]
    public Task<IActionResult> Attempt([FromQuery] Guid attemptId) => ControllerProcessor.ProcessAsync(() => checkout.ResumeAttemptAsync(attemptId), this);

    [AllowAnonymous, HttpPost("cancel"), EnableRateLimiting("stripe-status")]
    public Task<IActionResult> Cancel([FromBody] StripeSessionRequest request) => ControllerProcessor.ProcessAsync(() => checkout.CancelAsync(request.SessionId), this);

    [AllowAnonymous, HttpPost("webhook"), RequestSizeLimit(262144)]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body);
        await checkout.HandleWebhookAsync(await reader.ReadToEndAsync(), Request.Headers["Stripe-Signature"].ToString());
        return Ok();
    }
}
public record StripeSessionRequest(string SessionId);
