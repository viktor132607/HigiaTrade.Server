namespace HygiaTrade.API.Models;

public sealed record ProductCandidate(Guid Id, string Name, double Confidence);

public sealed record ExtractedInvoiceItem(
    string RawName,
    decimal Quantity,
    Guid? MatchedProductId,
    string? MatchedProductName,
    double MatchConfidence,
    double QuantityConfidence,
    IReadOnlyList<ProductCandidate> Candidates,
    string SourceLine);

public sealed record ExtractInvoiceResponse(
    string FileName,
    string DetectedLanguage,
    string? InvoiceNumber,
    string? InvoiceDate,
    bool DuplicateInvoice,
    IReadOnlyList<ExtractedInvoiceItem> Items,
    string TextPreview);

public sealed record ImportInvoiceItem(Guid ProductId, int Quantity);

public sealed record ImportInvoiceRequest(
    string InvoiceNumber,
    IReadOnlyList<ImportInvoiceItem> Items);

public sealed record ImportInvoiceProductResult(
    Guid Id,
    string Title,
    int AddedQuantity,
    uint CurrentQuantity);

public sealed record ImportInvoiceResponse(
    string InvoiceNumber,
    int ImportedProducts,
    int ImportedUnits,
    IReadOnlyList<ImportInvoiceProductResult> Items);
