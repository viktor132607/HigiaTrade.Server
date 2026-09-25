using HygiaTrade.API.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class InvoiceNumberServiceTests
{
    private readonly Mock<IInvoiceNumberPolicy> policy = new();
    private readonly Mock<IInvoiceNumberLookup> lookup = new();

    private InvoiceNumberService CreateService() =>
        new(policy.Object, lookup.Object);

    [Fact]
    public async Task CheckAsync_ReturnsFalseWithoutLookup_WhenNormalizedValueIsEmpty()
    {
        policy
            .Setup(x => x.NormalizeAndValidate("   "))
            .Returns(string.Empty);

        InvoiceNumberStatus result =
            await CreateService().CheckAsync(
                "   ",
                CancellationToken.None);

        Assert.False(result.Exists);

        lookup.Verify(
            x => x.ExistsAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CheckAsync_ReturnsLookupResult(
        bool exists)
    {
        const string raw = "  INV-001  ";
        const string normalized = "INV-001";

        using CancellationTokenSource cts = new();

        policy
            .Setup(x => x.NormalizeAndValidate(raw))
            .Returns(normalized);

        lookup
            .Setup(x => x.ExistsAsync(
                normalized,
                cts.Token))
            .ReturnsAsync(exists);

        InvoiceNumberStatus result =
            await CreateService().CheckAsync(
                raw,
                cts.Token);

        Assert.Equal(exists, result.Exists);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalseWithoutLookup_WhenNormalizedValueIsEmpty()
    {
        policy
            .Setup(x => x.NormalizeAndValidate(""))
            .Returns(string.Empty);

        bool result =
            await CreateService().ExistsAsync(
                "",
                CancellationToken.None);

        Assert.False(result);

        lookup.Verify(
            x => x.ExistsAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExistsAsync_ReturnsLookupResult(
        bool exists)
    {
        const string raw = " invoice-42 ";
        const string normalized = "invoice-42";

        using CancellationTokenSource cts = new();

        policy
            .Setup(x => x.NormalizeAndValidate(raw))
            .Returns(normalized);

        lookup
            .Setup(x => x.ExistsAsync(
                normalized,
                cts.Token))
            .ReturnsAsync(exists);

        bool result =
            await CreateService().ExistsAsync(
                raw,
                cts.Token);

        Assert.Equal(exists, result);
    }

    [Fact]
    public async Task CheckAsync_PropagatesPolicyException_WithoutLookup()
    {
        InvoiceNumberServiceException expected =
            new(
                400,
                "Invoice number cannot exceed 100 characters.");

        policy
            .Setup(x => x.NormalizeAndValidate(
                It.IsAny<string>()))
            .Throws(expected);

        InvoiceNumberServiceException actual =
            await Assert.ThrowsAsync<InvoiceNumberServiceException>(
                () => CreateService().CheckAsync(
                    new string('X', 101),
                    CancellationToken.None));

        Assert.Same(expected, actual);

        lookup.Verify(
            x => x.ExistsAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
