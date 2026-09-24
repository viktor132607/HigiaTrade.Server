using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using HygiaTrade.API.Controllers;

namespace HygiaTrade.API.Services;

public interface ITranslationService
{
    Task<TranslationResponse> TranslateBgToEnAsync(
        TranslationRequest request,
        CancellationToken cancellationToken);
}

public sealed class TranslationServiceException(
    int statusCode,
    object payload) : Exception
{
    public int StatusCode { get; } = statusCode;
    public object Payload { get; } = payload;
}

public sealed class TranslationService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : ITranslationService
{
    public async Task<TranslationResponse> TranslateBgToEnAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        string text =
            (request.Text ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            return new TranslationResponse
            {
                Translation = string.Empty
            };
        }

        if (text.Length > 3000)
        {
            throw new TranslationServiceException(
                StatusCodes.Status400BadRequest,
                new
                {
                    message = "Text is too long to translate."
                });
        }

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

        string apiBaseUrl =
            configuration["DEEPL_API_URL"] ??
            configuration["DeepL:ApiUrl"] ??
            "https://api-free.deepl.com";

        HttpClient client =
            httpClientFactory.CreateClient();

        using HttpRequestMessage message = new(
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

        DeepLResponse? payload =
            await response.Content
                .ReadFromJsonAsync<DeepLResponse>(
                    cancellationToken:
                        cancellationToken);

        string? translation =
            payload?
                .Translations?
                .FirstOrDefault()?
                .Text;

        if (string.IsNullOrWhiteSpace(translation))
        {
            throw new TranslationServiceException(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "Translation service returned an empty translation."
                });
        }

        return new TranslationResponse
        {
            Translation = translation.Trim()
        };
    }

    private sealed class DeepLResponse
    {
        [JsonPropertyName("translations")]
        public List<DeepLTranslation> Translations { get; set; } = [];
    }

    private sealed class DeepLTranslation
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
