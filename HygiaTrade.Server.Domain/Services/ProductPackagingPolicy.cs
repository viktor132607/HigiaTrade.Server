using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Domain.Services;

public static class ProductPackagingPolicy
{
    public static ProductPackagingOption? Resolve(Product product, Guid? optionId)
    {
        if (optionId is null) return null;
        var option = product.PackagingOptions.FirstOrDefault(x =>
            x.Id == optionId && x.ProductId == product.Id && !x.IsDeleted && x.IsActive);
        return option ?? throw new AppException("Selected packaging is not available.").SetStatusCode(409);
    }

    public static uint Available(Product product, ProductPackagingOption? option) =>
        option is null || option.IsDefault ? product.Quantity : option.StockQuantity;

    public static void Take(Product product, ProductPackagingOption? option, int quantity)
    {
        if (quantity < 1 || Available(product, option) < quantity)
            throw new AppException("Insufficient stock for the selected packaging.").SetStatusCode(409);
        if (option is null || option.IsDefault) product.Quantity -= (uint)quantity;
        else option.StockQuantity -= (uint)quantity;
    }

    public static void Restore(Product product, ProductPackagingOption? option, int quantity)
    {
        if (quantity < 1) return;
        if (option is null || option.IsDefault) product.Quantity += (uint)quantity;
        else option.StockQuantity += (uint)quantity;
    }
}
