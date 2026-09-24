using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class InvoiceNumberValidationControllerTests
{
    private readonly Mock<IInvoiceNumberService> invoiceNumberService = new();

    private InvoiceNumberValidationController CreateController() =>
        new(invoiceNumberService.Object);

    [Fact]
    public async Task CheckInvoiceNumberAsync_ReturnsOk_WithExistsFlag()
    {
        const string invoiceNumber = "INV-001";

        invoiceNumberService
            .Setup(service => service.CheckAsync(
                invoiceNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InvoiceNumberStatus(true));

        IActionResult result =
            await CreateController().CheckInvoiceNumberAsync(
                invoiceNumber,
                CancellationToken.None);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(
            true,
            ok.Value?.GetType().GetProperty("exists")?.GetValue(ok.Value));
    }

    [Fact]
    public async Task CheckInvoiceNumberAsync_ReturnsMappedError_WhenServiceThrows()
    {
        InvoiceNumberServiceException exception =
            new(400, "Invoice number cannot exceed 100 characters.");

        invoiceNumberService
            .Setup(service => service.CheckAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().CheckInvoiceNumberAsync(
                new string('X', 101),
                CancellationToken.None);

        ObjectResult error = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, error.StatusCode);
        Assert.Equal(
            exception.Message,
            error.Value?.GetType().GetProperty("message")?.GetValue(error.Value));
    }
}
