using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace HygiaTrade.API.Services;

public interface IDeepLTranslationParser
{
    Task<string> ParseAsync(
        HttpContent content,
        CancellationToken cancellationToken);
}

public sealed class DeepLTranslationParser
    : IDeepLTranslationParser
{
    public async Task<string> ParseAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        DeepLResponse? payload =
            await content.ReadFromJsonAsync<DeepLResponse>(
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

        return translation.Trim();
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
