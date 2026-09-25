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
    ITranslationRequestPolicy requestPolicy,
    IDeepLTranslationGateway gateway)
    : ITranslationService
{
    public async Task<TranslationResponse> TranslateBgToEnAsync(
        TranslationRequest request,
        CancellationToken cancellationToken)
    {
        string text =
            requestPolicy.Normalize(request.Text);

        if (text.Length == 0)
        {
            return new TranslationResponse
            {
                Translation = string.Empty
            };
        }

        string translation =
            await gateway.TranslateBgToEnAsync(
                text,
                cancellationToken);

        return new TranslationResponse
        {
            Translation = translation
        };
    }
}
