using HygiaTrade.Common.Responses.Order;
using HygiaTrade.Common.Responses.OrderItem;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Domain.Services;

public static class OrderMapping
{
    public static OrderResponse ToResponse(Order order) =>
        new()
        {
            DeliveryRegion = order.DeliveryRegion,
            PaymentMethod = order.PaymentMethod,
            DeliveryMethod = order.DeliveryMethod,
            InvoiceRequested = order.InvoiceRequested,
            InvoiceCompanyName = order.InvoiceCompanyName,
            InvoiceCompanyId = order.InvoiceCompanyId,
            InvoiceVatId = order.InvoiceVatId,
            InvoiceAddress = order.InvoiceAddress,

            Id = order.Id,
            PaymentStatus = order.PaymentStatus,
            UserId = order.UserId,
            OrderSubtotalExclVat =
                order.OrderSubtotalExclVat,
            OrderVatAmount = order.OrderVatAmount,
            OrderTotalPrice = order.OrderTotalPrice,
            Names = order.Names,
            PostalCode = order.PostalCode,
            Country = order.Country,
            City = order.City,
            Address = order.Address,
            Phone = order.Phone,
            Status = order.Status,
            CreatedOn = order.CreatedOn,
            Items = order.Items
                .Select(item =>
                    new OrderItemResponse
                    {
                        ProductId = item.ProductId,
                        SinglePrice = item.SinglePrice,
                        TotalPrice = item.TotalPrice,
                        SinglePriceExclVat =
                            item.SinglePriceExclVat,
                        TotalPriceExclVat =
                            item.TotalPriceExclVat,
                        VatAmount = item.VatAmount,
                        VatRate = item.VatRate,
                        PricingTier =
                            item.PricingTier.ToString(),
                        Quantity = item.Quantity,
                        PrimaryImageUri =
                            item.PrimaryImageUri,
                        Title = item.Title
                    })
                .OrderBy(item => item.Title)
                .ToList()
        };
}
