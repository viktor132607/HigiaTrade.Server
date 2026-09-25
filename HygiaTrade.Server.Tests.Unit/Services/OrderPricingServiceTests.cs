using HygiaTrade.Core.Enums;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class OrderPricingServiceTests
{
    private readonly Mock<IProductRepository> products = new();

    private OrderPricingService CreateService() =>
        new(products.Object);

    [Fact]
    public void ApplyCurrentPricing_SetsRetailVatSnapshot()
    {
        Product product = Product();
        product.RegularPrice = 12m;
        product.VatRate = 20m;

        var item = Item(product.Id, 2);

        CreateService().ApplyCurrentPricing(
            item,
            product,
            2);

        Assert.Equal(2, item.Quantity);
        Assert.Equal(12m, item.SinglePrice);
        Assert.Equal(10m, item.SinglePriceExclVat);
        Assert.Equal(24m, item.TotalPrice);
        Assert.Equal(20m, item.TotalPriceExclVat);
        Assert.Equal(4m, item.VatAmount);
        Assert.Equal(20m, item.VatRate);
        Assert.Equal(PricingTier.Retail, item.PricingTier);
    }

    [Fact]
    public async Task RefreshCurrentCartPricingAsync_Throws409_WhenProductMissing()
    {
        Guid productId = Guid.NewGuid();
        var order = new Order
        {
            Items = [Item(productId, 1)]
        };

        products
            .Setup(repository => repository.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        AppException exception =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService()
                    .RefreshCurrentCartPricingAsync(order));

        Assert.Equal(409, exception.StatusCode);
    }

    [Fact]
    public async Task RefreshCurrentCartPricingAsync_RepricesAndUpdatesTotals()
    {
        Product product = Product();
        product.RegularPrice = 12m;

        var order = new Order
        {
            Items = [Item(product.Id, 2)]
        };

        products
            .Setup(repository => repository.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        await CreateService()
            .RefreshCurrentCartPricingAsync(order);

        Assert.Equal(24m, order.OrderTotalPrice);
        Assert.Equal(20m, order.OrderSubtotalExclVat);
        Assert.Equal(4m, order.OrderVatAmount);
    }

    [Fact]
    public void UpdateOrderPrices_SumsRoundedSnapshots()
    {
        var order = new Order
        {
            Items =
            [
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    SinglePrice = 10,
                    TotalPrice = 10.005m,
                    TotalPriceExclVat = 8.335m,
                    VatAmount = 1.67m,
                    Title = "A",
                    PrimaryImageUri = "a"
                },
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 1,
                    SinglePrice = 5,
                    TotalPrice = 5.005m,
                    TotalPriceExclVat = 4.165m,
                    VatAmount = 0.84m,
                    Title = "B",
                    PrimaryImageUri = "b"
                }
            ]
        };

        CreateService().UpdateOrderPrices(order);

        Assert.Equal(15.01m, order.OrderTotalPrice);
        Assert.Equal(12.50m, order.OrderSubtotalExclVat);
        Assert.Equal(2.51m, order.OrderVatAmount);
    }

    private static Product Product() =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = "P",
            Description = string.Empty,
            MainImageUrl = "p"
        };

    private static OrderItem Item(
        Guid productId,
        int quantity) =>
        new()
        {
            ProductId = productId,
            Quantity = quantity,
            SinglePrice = 0,
            TotalPrice = 0,
            Title = "P",
            PrimaryImageUri = "p"
        };
}
