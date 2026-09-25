using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Pricing;

namespace HygiaTrade.Domain.Services;

public interface IOrderPricingService
{
    void ApplyCurrentPricing(
        OrderItem item,
        Product product,
        int quantity);

    Task RefreshCurrentCartPricingAsync(Order order);

    void UpdateOrderPrices(Order order);
}

public sealed class OrderPricingService(
    IProductRepository productRepository)
    : IOrderPricingService
{
    public void ApplyCurrentPricing(
        OrderItem item,
        Product product,
        int quantity)
    {
        ProductPriceBreakdown pricing =
            ProductPricingCalculator.Calculate(
                product,
                quantity);

        item.Quantity = quantity;
        item.SinglePrice = pricing.UnitPriceInclVat;
        item.SinglePriceExclVat =
            pricing.UnitPriceExclVat;

        item.TotalPrice =
            ProductPricingCalculator.RoundMoney(
                pricing.UnitPriceInclVat * quantity);

        item.TotalPriceExclVat =
            ProductPricingCalculator.GrossToNet(
                item.TotalPrice,
                pricing.VatRate);

        item.VatAmount =
            ProductPricingCalculator.RoundMoney(
                item.TotalPrice -
                item.TotalPriceExclVat);

        item.VatRate = pricing.VatRate;
        item.PricingTier = pricing.PricingTier;
    }

    public async Task RefreshCurrentCartPricingAsync(
        Order order)
    {
        foreach (OrderItem item in order.Items)
        {
            Product? product =
                await productRepository.GetByIdAsync(
                    item.ProductId);

            if (product is null)
            {
                throw new AppException(
                        $"Product '{item.Title}' is no longer available.")
                    .SetStatusCode(409);
            }

            ApplyCurrentPricing(
                item,
                product,
                item.Quantity);
        }

        UpdateOrderPrices(order);
    }

    public void UpdateOrderPrices(Order order)
    {
        order.OrderSubtotalExclVat =
            ProductPricingCalculator.RoundMoney(
                order.Items.Sum(item =>
                    item.TotalPriceExclVat));

        order.OrderVatAmount =
            ProductPricingCalculator.RoundMoney(
                order.Items.Sum(item =>
                    item.VatAmount));

        order.OrderTotalPrice =
            ProductPricingCalculator.RoundMoney(
                order.Items.Sum(item =>
                    item.TotalPrice));
    }
}
