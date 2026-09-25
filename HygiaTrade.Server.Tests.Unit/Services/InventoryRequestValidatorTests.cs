using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class InventoryRequestValidatorTests
{
    private readonly InventoryRequestValidator validator = new();

    [Fact]
    public void Normalize_TrimsInvoiceNumberAndPreservesQuantity()
    {
        InventoryStockRequest result =
            validator.Normalize(
                new AddStockRequest(
                    5,
                    "  INV-001  "));

        Assert.Equal(5, result.Quantity);
        Assert.Equal("INV-001", result.InvoiceNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Normalize_RejectsNonPositiveQuantity(
        int quantity)
    {
        InventoryServiceException ex =
            Assert.Throws<InventoryServiceException>(
                () => validator.Normalize(
                    new AddStockRequest(
                        quantity,
                        "INV")));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "Quantity must be greater than zero.",
            ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("     ")]
    public void Normalize_RejectsEmptyInvoiceNumber(
        string invoiceNumber)
    {
        InventoryServiceException ex =
            Assert.Throws<InventoryServiceException>(
                () => validator.Normalize(
                    new AddStockRequest(
                        1,
                        invoiceNumber)));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "Invoice number is required.",
            ex.Message);
    }

    [Fact]
    public void Normalize_RejectsInvoiceNumberLongerThanOneHundredCharacters()
    {
        InventoryServiceException ex =
            Assert.Throws<InventoryServiceException>(
                () => validator.Normalize(
                    new AddStockRequest(
                        1,
                        new string('x', 101))));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "Invoice number cannot exceed 100 characters.",
            ex.Message);
    }

    [Fact]
    public void Normalize_AcceptsOneHundredCharacterInvoiceNumber()
    {
        InventoryStockRequest result =
            validator.Normalize(
                new AddStockRequest(
                    1,
                    new string('x', 100)));

        Assert.Equal(100, result.InvoiceNumber.Length);
    }

    [Fact]
    public void EnsureCanAdd_AllowsMaximumExactQuantity()
    {
        validator.EnsureCanAdd(
            uint.MaxValue - 5,
            5);
    }

    [Fact]
    public void EnsureCanAdd_RejectsOverflow()
    {
        InventoryServiceException ex =
            Assert.Throws<InventoryServiceException>(
                () => validator.EnsureCanAdd(
                    uint.MaxValue,
                    1));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal(
            "The resulting quantity is too large.",
            ex.Message);
    }
}
