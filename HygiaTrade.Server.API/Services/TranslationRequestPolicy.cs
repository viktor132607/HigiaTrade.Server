namespace HygiaTrade.API.Services;

public interface ITranslationRequestPolicy
{
    string Normalize(string? text);
}

public sealed class TranslationRequestPolicy
    : ITranslationRequestPolicy
{
    public string Normalize(string? text)
    {
        string normalized =
            (text ?? string.Empty).Trim();

        if (normalized.Length > 3000)
        {
            throw new TranslationServiceException(
                StatusCodes.Status400BadRequest,
                new
                {
                    message = "Text is too long to translate."
                });
        }

        return normalized;
    }
}
