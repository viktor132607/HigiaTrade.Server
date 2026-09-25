using Microsoft.Extensions.Configuration;
namespace HygiaTrade.Common.Options;

public sealed class StripeOptions
{
    public string Mode { get; init; } = "Test";
    public string SecretKey { get; init; } = "";
    public string WebhookSecret { get; init; } = "";
    public string ClientUrl { get; init; } = "https://higiatrade.com";
    public bool LiveMode => Mode.Equals("Live", StringComparison.OrdinalIgnoreCase);
    public bool Enabled => (LiveMode || Mode.Equals("Test", StringComparison.OrdinalIgnoreCase))
        && SecretKey.StartsWith(LiveMode ? "sk_live_" : "sk_test_", StringComparison.Ordinal)
        && SecretKey.Length > 20;

    public static StripeOptions FromConfiguration(IConfiguration configuration)
    {
        string mode = configuration["STRIPE_MODE"] ?? configuration["Stripe:Mode"] ?? "Test";
        string suffix = mode.Equals("Live", StringComparison.OrdinalIgnoreCase) ? "LIVE" : "TEST";
        return new StripeOptions {
            Mode = mode,
            SecretKey = configuration[$"Stripe:SecretKey_{suffix}"] ?? "",
            WebhookSecret = configuration[$"Stripe:WebhookSecret_{suffix}"] ?? configuration["Stripe:WebhookSecret"] ?? "",
            ClientUrl = configuration["Stripe:ClientUrl"] ?? configuration["ClientApp:BaseUrl"] ?? "https://higiatrade.com"
        };
    }
}
