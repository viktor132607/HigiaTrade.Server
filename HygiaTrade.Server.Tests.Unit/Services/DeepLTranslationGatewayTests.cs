using System.Net;
using System.Net.Http;
using System.Text.Json;
using HygiaTrade.API.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class DeepLTranslationGatewayTests
{
    [Fact]
    public async Task TranslateBgToEnAsync_Throws503_WhenApiKeyIsMissing()
    {
        var configuration =
            new Mock<IConfiguration>();

        var gateway =
            CreateGateway(
                configuration.Object,
                new DelegateHandler(
                    _ => Task.FromResult(
                        new HttpResponseMessage(
                            HttpStatusCode.OK))),
                Mock.Of<IDeepLTranslationParser>());

        TranslationServiceException ex =
            await Assert.ThrowsAsync<TranslationServiceException>(
                () => gateway.TranslateBgToEnAsync(
                    "Здравей",
                    CancellationToken.None));

        Assert.Equal(503, ex.StatusCode);
        Assert.Equal(
            "DeepL translator is not configured. Set DEEPL_API_KEY on the server.",
            GetProperty(ex.Payload, "message"));
    }

    [Fact]
    public async Task TranslateBgToEnAsync_UsesLegacyConfigurationAndSendsExpectedRequest()
    {
        var configuration =
            Configuration(
                new Dictionary<string, string?>
                {
                    ["DeepL:ApiKey"] = "legacy-key",
                    ["DeepL:ApiUrl"] = "https://translator.example/"
                });

        HttpRequestSnapshot? captured = null;

        var handler = new DelegateHandler(
            async request =>
            {
                captured =
                    await HttpRequestSnapshot.CreateAsync(
                        request);

                return new HttpResponseMessage(
                    HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"translations":[{"text":"Hello"}]}""")
                };
            });

        var parser =
            new Mock<IDeepLTranslationParser>();

        parser
            .Setup(x => x.ParseAsync(
                It.IsAny<HttpContent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello");

        var gateway =
            CreateGateway(
                configuration,
                handler,
                parser.Object);

        string result =
            await gateway.TranslateBgToEnAsync(
                "Здравей",
                CancellationToken.None);

        Assert.Equal("Hello", result);
        Assert.NotNull(captured);

        Assert.Equal(
            "https://translator.example/v2/translate",
            captured!.Uri);

        Assert.Equal(
            HttpMethod.Post,
            captured.Method);

        Assert.Equal(
            "DeepL-Auth-Key",
            captured.AuthScheme);

        Assert.Equal(
            "legacy-key",
            captured.AuthParameter);

        using JsonDocument json =
            JsonDocument.Parse(captured.Body);

        JsonElement root = json.RootElement;

        Assert.Equal(
            "Здравей",
            root.GetProperty("text")[0].GetString());

        Assert.Equal(
            "BG",
            root.GetProperty("source_lang").GetString());

        Assert.Equal(
            "EN",
            root.GetProperty("target_lang").GetString());

        parser.Verify(
            x => x.ParseAsync(
                It.IsAny<HttpContent>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TranslateBgToEnAsync_PrefersEnvironmentStyleConfigurationAndUsesDefaultUrl()
    {
        var configuration =
            Configuration(
                new Dictionary<string, string?>
                {
                    ["DEEPL_API_KEY"] = "env-key",
                    ["DeepL:ApiKey"] = "legacy-key"
                });

        string? requestedUri = null;

        var handler = new DelegateHandler(
            request =>
            {
                requestedUri =
                    request.RequestUri!.ToString();

                return Task.FromResult(
                    new HttpResponseMessage(
                        HttpStatusCode.OK)
                    {
                        Content = new StringContent("{}")
                    });
            });

        var parser =
            new Mock<IDeepLTranslationParser>();

        parser
            .Setup(x => x.ParseAsync(
                It.IsAny<HttpContent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello");

        var gateway =
            CreateGateway(
                configuration,
                handler,
                parser.Object);

        await gateway.TranslateBgToEnAsync(
            "Здравей",
            CancellationToken.None);

        Assert.Equal(
            "https://api-free.deepl.com/v2/translate",
            requestedUri);
    }

    [Fact]
    public async Task TranslateBgToEnAsync_MapsProviderFailureAndPreservesShortDetails()
    {
        var configuration =
            Configuration(
                new Dictionary<string, string?>
                {
                    ["DEEPL_API_KEY"] = "key"
                });

        var handler = new DelegateHandler(
            _ => Task.FromResult(
                new HttpResponseMessage(
                    HttpStatusCode.TooManyRequests)
                {
                    Content =
                        new StringContent(
                            "rate limited")
                }));

        var gateway =
            CreateGateway(
                configuration,
                handler,
                Mock.Of<IDeepLTranslationParser>());

        TranslationServiceException ex =
            await Assert.ThrowsAsync<TranslationServiceException>(
                () => gateway.TranslateBgToEnAsync(
                    "Здравей",
                    CancellationToken.None));

        Assert.Equal(502, ex.StatusCode);
        Assert.Equal(
            "Translation service failed.",
            GetProperty(ex.Payload, "message"));

        Assert.Equal(
            "rate limited",
            GetProperty(ex.Payload, "details"));
    }

    [Fact]
    public async Task TranslateBgToEnAsync_TruncatesProviderFailureDetailsToFiveHundredCharacters()
    {
        var configuration =
            Configuration(
                new Dictionary<string, string?>
                {
                    ["DEEPL_API_KEY"] = "key"
                });

        string details = new('x', 600);

        var handler = new DelegateHandler(
            _ => Task.FromResult(
                new HttpResponseMessage(
                    HttpStatusCode.BadGateway)
                {
                    Content =
                        new StringContent(details)
                }));

        var gateway =
            CreateGateway(
                configuration,
                handler,
                Mock.Of<IDeepLTranslationParser>());

        TranslationServiceException ex =
            await Assert.ThrowsAsync<TranslationServiceException>(
                () => gateway.TranslateBgToEnAsync(
                    "Здравей",
                    CancellationToken.None));

        string mappedDetails =
            Assert.IsType<string>(
                GetProperty(
                    ex.Payload,
                    "details"));

        Assert.Equal(500, mappedDetails.Length);
        Assert.Equal(details[..500], mappedDetails);
    }

    private static DeepLTranslationGateway CreateGateway(
        IConfiguration configuration,
        HttpMessageHandler handler,
        IDeepLTranslationParser parser)
    {
        HttpClient client = new(handler);

        var factory =
            new Mock<IHttpClientFactory>();

        factory
            .Setup(x => x.CreateClient(
                It.IsAny<string>()))
            .Returns(client);

        return new DeepLTranslationGateway(
            factory.Object,
            configuration,
            parser);
    }

    private static IConfiguration Configuration(
        Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    private static object? GetProperty(
        object value,
        string name) =>
        value.GetType()
            .GetProperty(name)
            ?.GetValue(value);

    private sealed class DelegateHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            handler(request);
    }

    private sealed record HttpRequestSnapshot(
        string Uri,
        HttpMethod Method,
        string? AuthScheme,
        string? AuthParameter,
        string Body)
    {
        public static async Task<HttpRequestSnapshot> CreateAsync(
            HttpRequestMessage request) =>
            new(
                request.RequestUri!.ToString(),
                request.Method,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                await request.Content!.ReadAsStringAsync());
    }
}
