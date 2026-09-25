using System.Net.Http;
using System.Text;
using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class DeepLTranslationParserTests
{
    private readonly DeepLTranslationParser parser = new();

    [Fact]
    public async Task ParseAsync_ReturnsTrimmedFirstTranslation()
    {
        using StringContent content =
            JsonContent(
                """
                {
                  "translations": [
                    { "text": "  Hello  " },
                    { "text": "Second" }
                  ]
                }
                """);

        string result =
            await parser.ParseAsync(
                content,
                CancellationToken.None);

        Assert.Equal("Hello", result);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"translations":[]}""")]
    [InlineData("""{"translations":[{"text":""}]}""")]
    [InlineData("""{"translations":[{"text":"   "}]}""")]
    public async Task ParseAsync_Throws502_WhenTranslationIsMissingOrBlank(
        string json)
    {
        using StringContent content =
            JsonContent(json);

        TranslationServiceException ex =
            await Assert.ThrowsAsync<TranslationServiceException>(
                () => parser.ParseAsync(
                    content,
                    CancellationToken.None));

        Assert.Equal(502, ex.StatusCode);
        Assert.Equal(
            "Translation service returned an empty translation.",
            GetProperty(ex.Payload, "message"));
    }

    private static StringContent JsonContent(
        string json) =>
        new(
            json,
            Encoding.UTF8,
            "application/json");

    private static object? GetProperty(
        object value,
        string name) =>
        value.GetType()
            .GetProperty(name)
            ?.GetValue(value);
}
