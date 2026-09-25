using HygiaTrade.Core.Exceptions;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class BrandPolicyTests
{
    private readonly BrandPolicy policy = new();

    [Theory]
    [InlineData("  Acme  ", "Acme")]
    [InlineData("AB", "AB")]
    public void NormalizeName_TrimsValidName(
        string input,
        string expected)
    {
        Assert.Equal(
            expected,
            policy.NormalizeName(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("A")]
    public void NormalizeName_RejectsTooShortName(
        string? input)
    {
        AppException ex =
            Assert.Throws<AppException>(
                () => policy.NormalizeName(input));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public void NormalizeName_RejectsMoreThanEightyCharacters()
    {
        AppException ex =
            Assert.Throws<AppException>(
                () => policy.NormalizeName(
                    new string('x', 81)));

        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  value  ", "value")]
    public void NormalizeOptional_NormalizesExpected(
        string? input,
        string? expected)
    {
        Assert.Equal(
            expected,
            policy.NormalizeOptional(input));
    }

    [Fact]
    public void ThrowDuplicate_Throws409()
    {
        AppException ex =
            Assert.Throws<AppException>(
                policy.ThrowDuplicate);

        Assert.Equal(409, ex.StatusCode);
        Assert.Equal(
            "A brand with this name already exists.",
            ex.Message);
    }

    [Fact]
    public void ThrowNotFound_Throws404()
    {
        AppException ex =
            Assert.Throws<AppException>(
                () => policy.ThrowNotFound<object>());

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal(
            "Brand not found.",
            ex.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void EnsureCanDelete_AllowsZeroOrLess(
        int assignedProducts)
    {
        policy.EnsureCanDelete(
            assignedProducts);
    }

    [Fact]
    public void EnsureCanDelete_Throws409_WhenProductsAreAssigned()
    {
        AppException ex =
            Assert.Throws<AppException>(
                () => policy.EnsureCanDelete(2));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("2 product(s)", ex.Message);
    }
}
