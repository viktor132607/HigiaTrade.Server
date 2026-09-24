using HygiaTrade.API.Models;
using HygiaTrade.Data.Entities;
using Microsoft.AspNetCore.Http;

namespace HygiaTrade.API.Services;

public interface IInvoiceImportService
{
    Task<ExtractInvoiceResponse> ExtractAsync(
        IFormFile? file,
        CancellationToken cancellationToken);

    Task<ImportInvoiceResponse> CommitAsync(
        ImportInvoiceRequest request,
        CancellationToken cancellationToken);
}

public sealed class InvoiceImportServiceException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class InvoiceImportService(
    IInvoiceTextExtractor textExtractor,
    IInvoiceParser parser,
    IInvoiceImportRepository repository) : IInvoiceImportService
{
    public async Task<ExtractInvoiceResponse> ExtractAsync(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        InvoiceTextExtraction extraction =
            await textExtractor.ExtractAsync(file, cancellationToken);

        IReadOnlyList<InvoiceCatalogProduct> catalog =
            await repository.GetCatalogAsync(cancellationToken);

        ParsedInvoice parsed = parser.Parse(extraction.Text, catalog);

        bool duplicateInvoice =
            parsed.InvoiceNumber is not null &&
            await repository.InvoiceAlreadyImportedAsync(
                parsed.InvoiceNumber,
                cancellationToken);

        return new ExtractInvoiceResponse(
            extraction.FileName,
            parsed.DetectedLanguage,
            parsed.InvoiceNumber,
            parsed.InvoiceDate,
            duplicateInvoice,
            parsed.Items,
            extraction.Text.Length > 5000
                ? extraction.Text[..5000]
                : extraction.Text);
    }

    public async Task<ImportInvoiceResponse> CommitAsync(
        ImportInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        string invoiceNumber = request.InvoiceNumber?.Trim() ?? string.Empty;

        if (invoiceNumber.Length == 0)
        {
            throw new InvoiceImportServiceException(
                StatusCodes.Status400BadRequest,
                "Invoice number is required before importing stock.");
        }

        if (invoiceNumber.Length > 100)
        {
            throw new InvoiceImportServiceException(
                StatusCodes.Status400BadRequest,
                "Invoice number cannot exceed 100 characters.");
        }

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new InvoiceImportServiceException(
                StatusCodes.Status400BadRequest,
                "Choose at least one matched product to import.");
        }

        if (request.Items.Count > 200)
        {
            throw new InvoiceImportServiceException(
                StatusCodes.Status400BadRequest,
                "A single invoice can import up to 200 product rows.");
        }

        if (request.Items.Any(item => item.Quantity <= 0))
        {
            throw new InvoiceImportServiceException(
                StatusCodes.Status400BadRequest,
                "Every imported quantity must be greater than zero.");
        }

        if (await repository.InvoiceAlreadyImportedAsync(
            invoiceNumber,
            cancellationToken))
        {
            throw new InvoiceImportServiceException(
                StatusCodes.Status409Conflict,
                "This invoice number already has stock entries and cannot be imported twice.");
        }

        Dictionary<Guid, int> aggregated = request.Items
            .GroupBy(item => item.ProductId)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.Quantity));

        Guid[] productIds = aggregated.Keys.ToArray();

        IReadOnlyList<Product> products =
            await repository.GetProductsAsync(productIds, cancellationToken);

        if (products.Count != productIds.Length)
        {
            throw new InvoiceImportServiceException(
                StatusCodes.Status400BadRequest,
                "One or more selected products no longer exist.");
        }

        foreach (Product product in products)
        {
            int additionalQuantity = aggregated[product.Id];

            if ((ulong)product.Quantity + (ulong)additionalQuantity >
                uint.MaxValue)
            {
                throw new InvoiceImportServiceException(
                    StatusCodes.Status400BadRequest,
                    $"The resulting quantity for '{product.Title}' is too large.");
            }
        }

        DateTime createdOn = DateTime.UtcNow;

        await repository.ApplyStockImportAsync(
            products,
            aggregated,
            invoiceNumber,
            createdOn,
            cancellationToken);

        IReadOnlyList<ImportInvoiceProductResult> items = products
            .Select(product => new ImportInvoiceProductResult(
                product.Id,
                product.Title,
                aggregated[product.Id],
                product.Quantity))
            .ToList();

        return new ImportInvoiceResponse(
            invoiceNumber,
            products.Count,
            aggregated.Values.Sum(),
            items);
    }
}
