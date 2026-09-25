using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class InvoiceNumberPolicyTests
{
    private readonly InvoiceNumberPolicy policy = new();

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("  INV-001  ", "INV-001")]
    public void NormalizeAndValidate_NormalizesExpected(
        string? input,
        string expected)
    {
        Assert.Equal(
            expected,
            policy.NormalizeAndValidate(input));
    }

    [Fact]
    public void NormalizeAndValidate_AcceptsExactlyOneHundredCharacters()
    {
        string input = new('X', 100);

        Assert.Equal(
            input,
            policy.NormalizeAndValidate(input));
    }

    [Fact]
    public void NormalizeAndValidate_ValidatesAfterTrimming()
    {
        string input =
            "  " + new string('X', 100) + "  ";

        Assert.Equal(
            new string('X', 100),
            policy.NormalizeAndValidate(input));
    }

    [Fact]
    public void NormalizeAndValidate_RejectsMoreThanOneHundredCharacters()
    {
        InvoiceNumberServiceException ex =
            Assert.Throws<InvoiceNumberServiceException>(
                () => policy.NormalizeAndValidate(
                    new string('X', 101)));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "Invoice number cannot exceed 100 characters.",
            ex.Message);
    }
}
