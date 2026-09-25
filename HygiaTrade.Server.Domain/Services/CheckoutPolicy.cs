using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using System.Text.RegularExpressions;

namespace HygiaTrade.Domain.Services;

public static class CheckoutPolicy
{
    public const decimal MinimumOrderTotal = 50m;
    private static readonly string[] Regions = ["Русе", "Силистра", "Разград", "Свищов", "Бяла", "Търговище", "Ruse", "Silistra", "Razgrad", "Svishtov", "Byala", "Targovishte"];

    public static void Validate(CheckoutDetails request)
    {
        if (!new[] { "BG", "Bulgaria", "България" }.Contains(request.Country.Trim(), StringComparer.OrdinalIgnoreCase)
            || !Regions.Contains((request.DeliveryRegion ?? request.City).Trim(), StringComparer.OrdinalIgnoreCase))
            throw new AppException("Delivery address is outside the supported regions.").SetStatusCode(400);
        if (request.PaymentMethod?.Trim() is not ("bank-transfer" or "cash-on-delivery"))
            throw new AppException("Unsupported payment method.").SetStatusCode(400);
        if (request.DeliveryMethod?.Trim() != "regional-delivery")
            throw new AppException("Unsupported delivery method.").SetStatusCode(400);
        if (request.InvoiceRequested && (string.IsNullOrWhiteSpace(request.InvoiceCompanyName)
            || string.IsNullOrWhiteSpace(request.InvoiceAddress)
            || !Regex.IsMatch(request.InvoiceCompanyId?.Trim() ?? "", @"^(\d{9}|\d{13})$")
            || (!string.IsNullOrWhiteSpace(request.InvoiceVatId) && !Regex.IsMatch(request.InvoiceVatId.Trim(), @"^BG\d{9,10}$"))))
            throw new AppException("Invoice details are incomplete or invalid.").SetStatusCode(400);
    }

    public static void EnsureMinimum(decimal total)
    {
        if (total < MinimumOrderTotal)
            throw new AppException("Minimum order value is EUR 50.").SetStatusCode(400);
    }

    public static void Apply(Order order, CheckoutDetails request)
    {
        order.DeliveryRegion = (request.DeliveryRegion ?? request.City).Trim();
        order.PaymentMethod = request.PaymentMethod!.Trim();
        order.DeliveryMethod = request.DeliveryMethod!.Trim();
        order.InvoiceRequested = request.InvoiceRequested;
        order.InvoiceCompanyName = request.InvoiceRequested ? request.InvoiceCompanyName?.Trim() : null;
        order.InvoiceCompanyId = request.InvoiceRequested ? request.InvoiceCompanyId?.Trim() : null;
        order.InvoiceVatId = request.InvoiceRequested ? request.InvoiceVatId?.Trim() : null;
        order.InvoiceAddress = request.InvoiceRequested ? request.InvoiceAddress?.Trim() : null;
    }
}
