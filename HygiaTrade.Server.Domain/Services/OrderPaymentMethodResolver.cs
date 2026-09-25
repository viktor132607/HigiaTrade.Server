using HygiaTrade.Common.Options;
using HygiaTrade.Core.Exceptions;
using Microsoft.Extensions.Options;

namespace HygiaTrade.Domain.Services;

public interface IOrderPaymentMethodResolver
{
    string Resolve(string? paymentMethod);
}

public sealed class OrderPaymentMethodResolver(
    IOptions<PaymentOptions> paymentOptions)
    : IOrderPaymentMethodResolver
{
    private readonly PaymentOptions options =
        paymentOptions.Value;

    public string Resolve(string? paymentMethod)
    {
        string candidate =
            string.IsNullOrWhiteSpace(paymentMethod)
                ? options.SupportedMethods
                    .FirstOrDefault() ??
                  "online-card"
                : paymentMethod.Trim();

        if (!options.SupportedMethods.Contains(
                candidate,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new AppException(
                    "Unsupported payment method.")
                .SetStatusCode(400);
        }

        return candidate;
    }
}
