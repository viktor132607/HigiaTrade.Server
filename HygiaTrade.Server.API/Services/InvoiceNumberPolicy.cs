namespace HygiaTrade.API.Services;

public interface IInvoiceNumberPolicy
{
    string NormalizeAndValidate(
        string? invoiceNumber);
}

public sealed class InvoiceNumberPolicy
    : IInvoiceNumberPolicy
{
    public string NormalizeAndValidate(
        string? invoiceNumber)
    {
        string normalized =
            invoiceNumber?.Trim() ??
            string.Empty;

        if (normalized.Length > 100)
        {
            throw new InvoiceNumberServiceException(
                StatusCodes.Status400BadRequest,
                "Invoice number cannot exceed 100 characters.");
        }

        return normalized;
    }
}
