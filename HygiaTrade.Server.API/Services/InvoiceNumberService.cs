namespace HygiaTrade.API.Services;

public interface IInvoiceNumberService
{
    Task<InvoiceNumberStatus> CheckAsync(
        string? invoiceNumber,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        string invoiceNumber,
        CancellationToken cancellationToken);
}

public sealed record InvoiceNumberStatus(
    bool Exists);

public sealed class InvoiceNumberServiceException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class InvoiceNumberService(
    IInvoiceNumberPolicy policy,
    IInvoiceNumberLookup lookup)
    : IInvoiceNumberService
{
    public async Task<InvoiceNumberStatus> CheckAsync(
        string? invoiceNumber,
        CancellationToken cancellationToken)
    {
        string normalized =
            policy.NormalizeAndValidate(invoiceNumber);

        if (normalized.Length == 0)
        {
            return new InvoiceNumberStatus(false);
        }

        bool exists =
            await lookup.ExistsAsync(
                normalized,
                cancellationToken);

        return new InvoiceNumberStatus(exists);
    }

    public async Task<bool> ExistsAsync(
        string invoiceNumber,
        CancellationToken cancellationToken)
    {
        string normalized =
            policy.NormalizeAndValidate(invoiceNumber);

        if (normalized.Length == 0)
        {
            return false;
        }

        return await lookup.ExistsAsync(
            normalized,
            cancellationToken);
    }
}
