using HygiaTrade.Core.Enums;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.Tests.Unit.Services;

public sealed class OrderMappingTests
{
    [Fact]
    public void ToResponse_MapsOrderAndSortsItemsByTitle()
    {
        Guid userId = Guid.NewGuid();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderSubtotalExclVat = 12m,
            OrderVatAmount = 3m,
            OrderTotalPrice = 15m,
            Names = "User",
            PostalCode = "7000",
            Country = "BG",
            City = "Ruse",
            Address = "Street",
            Phone = "1",
            Status = OrderStatus.Processing,
            CreatedOn = new DateTime(2026, 9, 25),
            Items =
            [
                Item("Zulu"),
                Item("Alpha")
            ]
        };

        var result = OrderMapping.ToResponse(order);

        Assert.Equal(order.Id, result.Id);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(15m, result.OrderTotalPrice);
        Assert.Equal("User", result.Names);
        Assert.Equal(OrderStatus.Processing, result.Status);
        Assert.Equal("Alpha", result.Items.First().Title);
        Assert.Equal("Zulu", result.Items.Last().Title);
    }

    private static OrderItem Item(string title) =>
        new()
        {
            ProductId = Guid.NewGuid(),
            Quantity = 2,
            SinglePrice = 5m,
            TotalPrice = 10m,
            SinglePriceExclVat = 4.17m,
            TotalPriceExclVat = 8.33m,
            VatAmount = 1.67m,
            VatRate = 20m,
            PricingTier = PricingTier.Retail,
            Title = title,
            PrimaryImageUri = "img"
        };
}
