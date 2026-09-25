using HygiaTrade.API.Controllers;

namespace HygiaTrade.API.Services;

public interface IInventoryService
{
    Task<InventoryResponse> GetAsync(Guid productId);

    Task<AddStockResponse> AddAsync(
        Guid productId,
        AddStockRequest request);
}

public sealed class InventoryServiceException(
    int statusCode,
    string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class InventoryService(
    IInventoryReader reader,
    IInventoryRequestValidator validator,
    IInventoryStockAdder stockAdder)
    : IInventoryService
{
    public async Task<InventoryResponse> GetAsync(
        Guid productId)
    {
        uint? currentQuantity =
            await reader.GetProductQuantityAsync(
                productId);

        if (currentQuantity is null)
        {
            throw new InventoryServiceException(
                StatusCodes.Status404NotFound,
                "Product not found.");
        }

        IReadOnlyList<StockEntryResponse> entries =
            await reader.GetEntriesAsync(
                productId);

        return new InventoryResponse(
            currentQuantity.Value,
            entries);
    }

    public async Task<AddStockResponse> AddAsync(
        Guid productId,
        AddStockRequest request)
    {
        InventoryStockRequest normalized =
            validator.Normalize(request);

        InventoryStockAdjustment result =
            await stockAdder.AddAsync(
                productId,
                normalized);

        return new AddStockResponse(
            result.CurrentQuantity,
            result.Entry);
    }
}
