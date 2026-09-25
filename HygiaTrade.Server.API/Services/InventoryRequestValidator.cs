using HygiaTrade.API.Controllers;

namespace HygiaTrade.API.Services;

public sealed record InventoryStockRequest(
    int Quantity,
    string InvoiceNumber);

public interface IInventoryRequestValidator
{
    InventoryStockRequest Normalize(
        AddStockRequest request);

    void EnsureCanAdd(
        uint currentQuantity,
        int quantity);
}

public sealed class InventoryRequestValidator
    : IInventoryRequestValidator
{
    public InventoryStockRequest Normalize(
        AddStockRequest request)
    {
        if (request.Quantity <= 0)
        {
            throw new InventoryServiceException(
                StatusCodes.Status400BadRequest,
                "Quantity must be greater than zero.");
        }

        string invoiceNumber =
            request.InvoiceNumber?.Trim() ??
            string.Empty;

        if (invoiceNumber.Length == 0)
        {
            throw new InventoryServiceException(
                StatusCodes.Status400BadRequest,
                "Invoice number is required.");
        }

        if (invoiceNumber.Length > 100)
        {
            throw new InventoryServiceException(
                StatusCodes.Status400BadRequest,
                "Invoice number cannot exceed 100 characters.");
        }

        return new InventoryStockRequest(
            request.Quantity,
            invoiceNumber);
    }

    public void EnsureCanAdd(
        uint currentQuantity,
        int quantity)
    {
        if ((ulong)currentQuantity +
            (ulong)quantity >
            uint.MaxValue)
        {
            throw new InventoryServiceException(
                StatusCodes.Status400BadRequest,
                "The resulting quantity is too large.");
        }
    }
}
