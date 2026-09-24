using HygiaTrade.API.Controllers;
using HygiaTrade.API.Models;
using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class InvoiceImportControllerTests
{
    private readonly Mock<IInvoiceImportService> invoiceImportService = new();

    private InvoiceImportController CreateController() =>
        new(invoiceImportService.Object);

    [Fact]
    public async Task ExtractAsync_ReturnsOk_WithExtractedInvoice()
    {
        IFormFile file =
            CreateFile([1, 2, 3], "invoice.pdf");

        ExtractInvoiceResponse expected =
            new(
                "invoice.pdf",
                "bg",
                "INV-001",
                "25.09.2026",
                false,
                [],
                "preview");

        invoiceImportService
            .Setup(service => service.ExtractAsync(
                file,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().ExtractAsync(
                file,
                CancellationToken.None);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task ExtractAsync_ReturnsMappedStatus_WhenServiceExceptionOccurs()
    {
        InvoiceImportServiceException exception =
            new(
                StatusCodes.Status422UnprocessableEntity,
                "No readable invoice text was detected.");

        invoiceImportService
            .Setup(service => service.ExtractAsync(
                It.IsAny<IFormFile?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().ExtractAsync(
                null,
                CancellationToken.None);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status422UnprocessableEntity,
            error.StatusCode);

        Assert.Equal(
            exception.Message,
            GetProperty(error.Value, "message"));
    }

    [Fact]
    public async Task ExtractAsync_Returns503_WhenRequiredExecutableIsMissing()
    {
        FileNotFoundException exception =
            new("Required OCR executable is not available.");

        invoiceImportService
            .Setup(service => service.ExtractAsync(
                It.IsAny<IFormFile?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().ExtractAsync(
                null,
                CancellationToken.None);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status503ServiceUnavailable,
            error.StatusCode);

        Assert.Equal(
            exception.Message,
            GetProperty(error.Value, "message"));
    }

    [Fact]
    public async Task ExtractAsync_Returns504_WhenOcrTimesOut()
    {
        TimeoutException exception =
            new("Invoice OCR timed out.");

        invoiceImportService
            .Setup(service => service.ExtractAsync(
                It.IsAny<IFormFile?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().ExtractAsync(
                null,
                CancellationToken.None);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status504GatewayTimeout,
            error.StatusCode);

        Assert.Equal(
            exception.Message,
            GetProperty(error.Value, "message"));
    }

    [Fact]
    public async Task CommitAsync_ReturnsOk_WithImportResult()
    {
        ImportInvoiceRequest request =
            new(
                "INV-001",
                [
                    new ImportInvoiceItem(
                        Guid.NewGuid(),
                        5)
                ]);

        ImportInvoiceResponse expected =
            new(
                "INV-001",
                1,
                5,
                []);

        invoiceImportService
            .Setup(service => service.CommitAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().CommitAsync(
                request,
                CancellationToken.None);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task CommitAsync_ReturnsMappedStatus_WhenServiceExceptionOccurs()
    {
        ImportInvoiceRequest request =
            new(
                "INV-001",
                []);

        InvoiceImportServiceException exception =
            new(
                StatusCodes.Status400BadRequest,
                "At least one item is required.");

        invoiceImportService
            .Setup(service => service.CommitAsync(
                request,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().CommitAsync(
                request,
                CancellationToken.None);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            error.StatusCode);

        Assert.Equal(
            exception.Message,
            GetProperty(error.Value, "message"));
    }

    private static IFormFile CreateFile(
        byte[] data,
        string fileName)
    {
        MemoryStream stream = new(data);

        return new FormFile(
            stream,
            0,
            data.Length,
            "file",
            fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private static object? GetProperty(
        object? value,
        string name)
    {
        Assert.NotNull(value);

        return value
            .GetType()
            .GetProperty(name)
            ?.GetValue(value);
    }
}
