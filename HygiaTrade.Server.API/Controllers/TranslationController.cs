using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HygiaTrade.Core.StaticClasses;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/translation")]
public sealed class TranslationController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : ControllerBase
{
    [HttpPost("bg-to-en")]
    public async Task<ActionResult<TranslationResponse>> TranslateBgToEnAsync(
        [FromBody] TranslationRequest request,
        CancellationToken cancellationToken)
    {
        string text = (request.Text ?? string.Empty).Trim();

        if (text.Length == 0)
        {
            return Ok(new TranslationResponse { Translation = string.Empty });
        }

        if (text.Length > 3000)
        {
            return BadRequest(new { message = "Text is too long to translate." });
        }

        string? apiKey =
            configuration["DEEPL_API_KEY"] ??
            configuration["DeepL:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "DeepL translator is not configured. Set DEEPL_API_KEY on the server." });
        }

        string apiBaseUrl =
            configuration["DEEPL_API_URL"] ??
            configuration["DeepL:ApiUrl"] ??
            "https://api-free.deepl.com";

        HttpClient client = httpClientFactory.CreateClient();

        using HttpRequestMessage message = new(
            HttpMethod.Post,
            $"{apiBaseUrl.TrimEnd('/')}/v2/translate");

        message.Headers.Authorization =
            new AuthenticationHeaderValue("DeepL-Auth-Key", apiKey);

        message.Content = JsonContent.Create(new
        {
            text = new[] { text },
            source_lang = "BG",
            target_lang = "EN"
        });

        using HttpResponseMessage response =
            await client.SendAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string details = await response.Content.ReadAsStringAsync(cancellationToken);
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new
                {
                    message = "Translation service failed.",
                    details = details.Length <= 500 ? details : details[..500]
                });
        }

        DeepLResponse? payload =
            await response.Content.ReadFromJsonAsync<DeepLResponse>(cancellationToken: cancellationToken);

        string? translation = payload?.Translations?.FirstOrDefault()?.Text;

        if (string.IsNullOrWhiteSpace(translation))
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = "Translation service returned an empty translation." });
        }

        return Ok(new TranslationResponse { Translation = translation.Trim() });
    }
}

public sealed class TranslationRequest
{
    public string? Text { get; set; }
}

public sealed class TranslationResponse
{
    public string Translation { get; set; } = string.Empty;
}

public sealed class DeepLResponse
{
    [JsonPropertyName("translations")]
    public List<DeepLTranslation> Translations { get; set; } = [];
}

public sealed class DeepLTranslation
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
