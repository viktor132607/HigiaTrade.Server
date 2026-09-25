using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class TranslationRequestPolicyTests
{
    private readonly TranslationRequestPolicy policy = new();

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("  текст  ", "текст")]
    public void Normalize_TrimsAndNormalizesEmptyValues(
        string? input,
        string expected)
    {
        Assert.Equal(
            expected,
            policy.Normalize(input));
    }

    [Fact]
    public void Normalize_AcceptsExactlyThreeThousandCharacters()
    {
        string input = new('x', 3000);

        Assert.Equal(
            input,
            policy.Normalize(input));
    }

    [Fact]
    public void Normalize_RejectsMoreThanThreeThousandCharacters()
    {
        TranslationServiceException ex =
            Assert.Throws<TranslationServiceException>(
                () => policy.Normalize(
                    new string('x', 3001)));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "Text is too long to translate.",
            GetProperty(ex.Payload, "message"));
    }

    private static object? GetProperty(
        object value,
        string name) =>
        value.GetType()
            .GetProperty(name)
            ?.GetValue(value);
}
