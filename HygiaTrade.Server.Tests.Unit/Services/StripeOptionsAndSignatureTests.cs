using System.Security.Cryptography;
using System.Text;
using HygiaTrade.API.Services;
using HygiaTrade.Common.Options;
using HygiaTrade.Core.Exceptions;
using Microsoft.Extensions.Configuration;

namespace HygiaTrade.Tests.Unit.Services;
public class StripeOptionsAndSignatureTests
{
    [Fact]
    public void UsesExistingRenderVariableNamesWithoutLiveFallback()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["STRIPE_MODE"] = "Test", ["STRIPE:SECRETKEY_TEST"] = "sk_test_example_only_123456", ["STRIPE:SECRETKEY_LIVE"] = "sk_live_example_only_123456"
        }).Build();
        var options = StripeOptions.FromConfiguration(config);
        Assert.True(options.Enabled); Assert.False(options.LiveMode); Assert.StartsWith("sk_test_", options.SecretKey);
        config["STRIPE:SECRETKEY_TEST"] = null;
        Assert.False(StripeOptions.FromConfiguration(config).Enabled);
        config["STRIPE_MODE"] = "Live";
        Assert.True(StripeOptions.FromConfiguration(config).LiveMode);
    }
    [Fact]
    public void RejectsMissingWrongAndStaleSignatures()
    {
        var gateway = new StripeGateway(new StripeOptions { WebhookSecret = "whsec_example" });
        Assert.Throws<AppException>(() => gateway.ReadWebhookSession("{}", ""));
        Assert.Throws<AppException>(() => gateway.ReadWebhookSession("{}", "t=1,v1=wrong"));
        Assert.Throws<AppException>(() => gateway.ReadWebhookSession("{}", Sign("{}", 1)));
    }
    [Fact]
    public void AcceptsSignedCheckoutEventAndRejectsChangedBody()
    {
        var gateway = new StripeGateway(new StripeOptions { WebhookSecret = "whsec_example" });
        const string body = "{\"id\":\"evt_test\",\"object\":\"event\",\"type\":\"checkout.session.completed\",\"livemode\":false,\"data\":{\"object\":{\"id\":\"cs_test_123\",\"object\":\"checkout.session\"}}}";
        string signature = Sign(body, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        Assert.Equal("cs_test_123", gateway.ReadWebhookSession(body, signature));
        Assert.Throws<AppException>(() => gateway.ReadWebhookSession(body.Replace("123", "999"), signature));
    }
    private static string Sign(string body, long timestamp) => $"t={timestamp},v1={Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("whsec_example"), Encoding.UTF8.GetBytes($"{timestamp}.{body}"))).ToLowerInvariant()}";
}
