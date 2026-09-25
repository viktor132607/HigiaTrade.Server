using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HygiaTrade.API.Services;

public interface IDeepLTranslationGateway
{
    Task<string> TranslateBgToEnAsync(
        string text,
        CancellationToken cancellationToken);
}

public sealed class DeepLTranslationGateway(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    IDeepLTranslationParser parser)
    : IDeepLTranslationGateway
{
    public async Task<string> TranslateBgToEnAsync(
        string text,
        CancellationToken cancellationToken)
    {
        string apiKey = GetApiKey();
        string apiBaseUrl = GetApiBaseUrl();

        HttpClient client =
            httpClientFactory.CreateClient();

        using HttpRequestMessage message =
            CreateRequest(
                apiBaseUrl,
                apiKey,
                text);

        using HttpResponseMessage response =
            await client.SendAsync(
                message,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string details =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            throw new TranslationServiceException(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "Translation service failed.",
                    details =
                        details.Length <= 500
                            ? details
                            : details[..500]
                });
        }

        return await parser.ParseAsync(
            response.Content,
            cancellationToken);
    }

    private string GetApiKey()
    {
        string? apiKey =
            configuration["DEEPL_API_KEY"] ??
            configuration["DeepL:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new TranslationServiceException(
                StatusCodes.Status503ServiceUnavailable,
                new
                {
                    message = "DeepL translator is not configured. Set DEEPL_API_KEY on the server."
                });
        }

        return apiKey;
    }

    private string GetApiBaseUrl() =>
        configuration["DEEPL_API_URL"] ??
        configuration["DeepL:ApiUrl"] ??
        "https://api-free.deepl.com";

    private static HttpRequestMessage CreateRequest(
        string apiBaseUrl,
        string apiKey,
        string text)
    {
        HttpRequestMessage message = new(
            HttpMethod.Post,
            $"{apiBaseUrl.TrimEnd('/')}/v2/translate");

        message.Headers.Authorization =
            new AuthenticationHeaderValue(
                "DeepL-Auth-Key",
                apiKey);

        message.Content = JsonContent.Create(new
        {
            text = new[] { text },
            source_lang = "BG",
            target_lang = "EN"
        });

        return message;
    }
}
