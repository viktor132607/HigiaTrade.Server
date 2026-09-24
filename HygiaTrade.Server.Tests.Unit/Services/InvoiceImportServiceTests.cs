using HygiaTrade.API.Models;
using HygiaTrade.API.Services;
using HygiaTrade.Data.Entities;
using Microsoft.AspNetCore.Http;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class InvoiceImportServiceTests
{
    private readonly Mock<IInvoiceTextExtractor> extractor = new();
    private readonly Mock<IInvoiceParser> parser = new();
    private readonly Mock<IInvoiceImportRepository> repository = new();

    private InvoiceImportService CreateService() =>
        new(
            extractor.Object,
            parser.Object,
            repository.Object);

    [Fact]
    public async Task ExtractAsync_OrchestratesExtractionParsingAndDuplicateCheck()
    {
        Guid productId = Guid.NewGuid();
        var file = new Mock<IFormFile>();
        var extraction = new InvoiceTextExtraction(
            "invoice.pdf",
            "Invoice No: INV-123\nDate: 24.09.2026\nWidget 2 pcs");

        var catalog = new[]
        {
            new InvoiceCatalogProduct(productId, "Widget")
        };

        var parsed = new ParsedInvoice(
            "INV-123",
            "24.09.2026",
            "en",
            new[]
            {
                new ExtractedInvoiceItem(
                    "Widget",
                    2,
                    productId,
                    "Widget",
                    0.99,
                    0.98,
                    Array.Empty<ProductCandidate>(),
                    "Widget 2 pcs")
            });

        extractor
            .Setup(service => service.ExtractAsync(
                file.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extraction);

        repository
            .Setup(service => service.GetCatalogAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);

        parser
            .Setup(service => service.Parse(
                extraction.Text,
                catalog))
            .Returns(parsed);

        repository
            .Setup(service => service.InvoiceAlreadyImportedAsync(
                "INV-123",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        ExtractInvoiceResponse result =
            await CreateService().ExtractAsync(
                file.Object,
                CancellationToken.None);

        Assert.Equal("invoice.pdf", result.FileName);
        Assert.Equal("INV-123", result.InvoiceNumber);
        Assert.Equal("24.09.2026", result.InvoiceDate);
        Assert.Equal("en", result.DetectedLanguage);
        Assert.True(result.DuplicateInvoice);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task ExtractAsync_SkipsDuplicateCheck_WhenInvoiceNumberMissing()
    {
        var file = new Mock<IFormFile>();
        var extraction = new InvoiceTextExtraction(
            "invoice.png",
            new string('a', 5001));

        extractor
            .Setup(service => service.ExtractAsync(
                file.Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extraction);

        repository
            .Setup(service => service.GetCatalogAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<InvoiceCatalogProduct>());

        parser
            .Setup(service => service.Parse(
                extraction.Text,
                It.IsAny<IReadOnlyList<InvoiceCatalogProduct>>()))
            .Returns(new ParsedInvoice(
                null,
                null,
                "en",
                Array.Empty<ExtractedInvoiceItem>()));

        ExtractInvoiceResponse result =
            await CreateService().ExtractAsync(
                file.Object,
                CancellationToken.None);

        Assert.False(result.DuplicateInvoice);
        Assert.Equal(5000, result.TextPreview.Length);

        repository.Verify(
            service => service.InvoiceAlreadyImportedAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("", 400, "Invoice number is required before importing stock.")]
    [InlineData("   ", 400, "Invoice number is required before importing stock.")]
    public async Task CommitAsync_RejectsMissingInvoiceNumber(
        string invoiceNumber,
        int statusCode,
        string message)
    {
        var request = new ImportInvoiceRequest(
            invoiceNumber,
            new[]
            {
                new ImportInvoiceItem(Guid.NewGuid(), 1)
            });

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => CreateService().CommitAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public async Task CommitAsync_RejectsInvoiceNumberLongerThan100Characters()
    {
        var request = new ImportInvoiceRequest(
            new string('X', 101),
            new[]
            {
                new ImportInvoiceItem(Guid.NewGuid(), 1)
            });

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => CreateService().CommitAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task CommitAsync_RejectsEmptyItems()
    {
        var request = new ImportInvoiceRequest(
            "INV-1",
            Array.Empty<ImportInvoiceItem>());

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => CreateService().CommitAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task CommitAsync_RejectsNullItems()
    {
        var request = new ImportInvoiceRequest(
            "INV-1",
            null!);

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => CreateService().CommitAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task CommitAsync_RejectsMoreThan200Rows()
    {
        ImportInvoiceItem[] items = Enumerable
            .Range(0, 201)
            .Select(_ => new ImportInvoiceItem(Guid.NewGuid(), 1))
            .ToArray();

        var request = new ImportInvoiceRequest("INV-1", items);

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => CreateService().CommitAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CommitAsync_RejectsNonPositiveQuantity(int quantity)
    {
        var request = new ImportInvoiceRequest(
            "INV-1",
            new[]
            {
                new ImportInvoiceItem(Guid.NewGuid(), quantity)
            });

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => CreateService().CommitAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task CommitAsync_RejectsDuplicateInvoice()
    {
        repository
            .Setup(service => service.InvoiceAlreadyImportedAsync(
                "INV-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new ImportInvoiceRequest(
            " INV-1 ",
            new[]
            {
                new ImportInvoiceItem(Guid.NewGuid(), 1)
            });

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => CreateService().CommitAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
    }

    [Fact]
    public async Task CommitAsync_RejectsMissingProducts()
    {
        Guid productId = Guid.NewGuid();

        repository
            .Setup(service => service.InvoiceAlreadyImportedAsync(
                "INV-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        repository
            .Setup(service => service.GetProductsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Product>());

        var request = new ImportInvoiceRequest(
            "INV-1",
            new[]
            {
                new ImportInvoiceItem(productId, 1)
            });

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => CreateService().CommitAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Equal(
            "One or more selected products no longer exist.",
            exception.Message);
    }

    [Fact]
    public async Task CommitAsync_RejectsQuantityOverflow()
    {
        Guid productId = Guid.NewGuid();

        repository
            .Setup(service => service.InvoiceAlreadyImportedAsync(
                "INV-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        repository
            .Setup(service => service.GetProductsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Product
                {
                    Id = productId,
                    Title = "Widget",
                    Quantity = uint.MaxValue
                }
            });

        var request = new ImportInvoiceRequest(
            "INV-1",
            new[]
            {
                new ImportInvoiceItem(productId, 1)
            });

        InvoiceImportServiceException exception =
            await Assert.ThrowsAsync<InvoiceImportServiceException>(
                () => CreateService().CommitAsync(
                    request,
                    CancellationToken.None));

        Assert.Equal(StatusCodes.Status400BadRequest, exception.StatusCode);
        Assert.Contains("Widget", exception.Message);
    }

    [Fact]
    public async Task CommitAsync_AggregatesRowsAndReturnsImportedProducts()
    {
        Guid productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Title = "Widget",
            Quantity = 5
        };

        repository
            .Setup(service => service.InvoiceAlreadyImportedAsync(
                "INV-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        repository
            .Setup(service => service.GetProductsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { product });

        repository
            .Setup(service => service.ApplyStockImportAsync(
                It.IsAny<IReadOnlyList<Product>>(),
                It.IsAny<IReadOnlyDictionary<Guid, int>>(),
                "INV-1",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback<
                IReadOnlyList<Product>,
                IReadOnlyDictionary<Guid, int>,
                string,
                DateTime,
                CancellationToken>(
                (products, quantities, _, createdOn, _) =>
                {
                    foreach (Product item in products)
                    {
                        item.Quantity += (uint)quantities[item.Id];
                        item.ModifiedOn = createdOn;
                    }
                })
            .Returns(Task.CompletedTask);

        var request = new ImportInvoiceRequest(
            " INV-1 ",
            new[]
            {
                new ImportInvoiceItem(productId, 2),
                new ImportInvoiceItem(productId, 3)
            });

        ImportInvoiceResponse result =
            await CreateService().CommitAsync(
                request,
                CancellationToken.None);

        Assert.Equal("INV-1", result.InvoiceNumber);
        Assert.Equal(1, result.ImportedProducts);
        Assert.Equal(5, result.ImportedUnits);

        ImportInvoiceProductResult imported = Assert.Single(result.Items);
        Assert.Equal(productId, imported.Id);
        Assert.Equal(5, imported.AddedQuantity);
        Assert.Equal((uint)10, imported.CurrentQuantity);
    }
}
