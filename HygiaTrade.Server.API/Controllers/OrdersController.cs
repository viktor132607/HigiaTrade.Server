using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.API.Helpers;
using HygiaTrade.Common.Requests.Order;
using HygiaTrade.Common.Requests.OrderItem;
using HygiaTrade.Core.Enums;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Interfaces;
using HygiaTrade.Domain.Pricing;

namespace HygiaTrade.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderService orderService, ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync() => await ControllerProcessor.ProcessAsync(() => orderService.GetAsync(), this);

    [HttpPut]
    public async Task<IActionResult> AddProductAsync([FromBody] AddOrderItemRequest request) => await ControllerProcessor.ProcessAsync(() => orderService.AddProductAsync(request), this, true);

    [HttpDelete]
    public async Task<IActionResult> RemoveProductAsync([FromBody] RemoveOrderItemRequest request) => await ControllerProcessor.ProcessAsync(() => orderService.RemoveProductAsync(request), this, true);

    [HttpPost]
    public async Task<IActionResult> SendOrder([FromBody] SendOrderRequest request) => await ControllerProcessor.ProcessAsync(() => orderService.SendCurrentAsync(request), this);

    [AllowAnonymous]
    [HttpPost("guest")]
    public async Task<IActionResult> SendGuestOrder([FromBody] GuestOrderRequest request)
    {
        if (!request.ConsentAccepted) return BadRequest("Consent is required to place an order.");
        if (request.Items.Count == 0) return BadRequest("Cart is empty.");

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await dbContext.Products.Where(p => productIds.Contains(p.Id) && p.IsActive).ToDictionaryAsync(p => p.Id);
        if (products.Count != productIds.Count) return Conflict("One or more products are no longer available.");

        var order = new Order
        {
            GuestEmail = request.Email.Trim(),
            Names = request.Names.Trim(),
            PostalCode = request.PostalCode.Trim(),
            Country = request.Country.Trim(),
            City = request.City.Trim(),
            Address = request.Address.Trim(),
            Phone = request.Phone.Trim(),
            Status = OrderStatus.PendingVerification
        };

        foreach (var requestedItem in request.Items)
        {
            var product = products[requestedItem.ProductId];
            if (requestedItem.Quantity < 1 || product.Quantity < requestedItem.Quantity)
                return Conflict($"Insufficient stock for '{product.Title}'.");

            var pricing = ProductPricingCalculator.Calculate(product, requestedItem.Quantity);
            var totalInclVat = ProductPricingCalculator.RoundMoney(pricing.UnitPriceInclVat * requestedItem.Quantity);
            var totalExclVat = ProductPricingCalculator.GrossToNet(totalInclVat, pricing.VatRate);

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = requestedItem.Quantity,
                SinglePrice = pricing.UnitPriceInclVat,
                TotalPrice = totalInclVat,
                SinglePriceExclVat = pricing.UnitPriceExclVat,
                TotalPriceExclVat = totalExclVat,
                VatAmount = ProductPricingCalculator.RoundMoney(totalInclVat - totalExclVat),
                VatRate = pricing.VatRate,
                PricingTier = pricing.PricingTier,
                Title = product.Title,
                PrimaryImageUri = product.MainImageUrl
            });

            product.Quantity -= (uint)requestedItem.Quantity;
        }

        order.OrderSubtotalExclVat = ProductPricingCalculator.RoundMoney(order.Items.Sum(i => i.TotalPriceExclVat));
        order.OrderVatAmount = ProductPricingCalculator.RoundMoney(order.Items.Sum(i => i.VatAmount));
        order.OrderTotalPrice = ProductPricingCalculator.RoundMoney(order.Items.Sum(i => i.TotalPrice));

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        return Ok(new { orderId = order.Id });
    }

    [HttpGet("get-list")]
    public async Task<IActionResult> SearchOrdersAsync([FromQuery] SearchOrderRequest? request) => await ControllerProcessor.ProcessAsync(() => orderService.SearchOrdersAsync(request ?? new SearchOrderRequest()), this, true);

    [HttpPut("change-status")]
    public async Task<IActionResult> ChangeStatusAsync([FromBody] ChangeOrderStatusRequest request) => await ControllerProcessor.ProcessAsync(() => orderService.ChangeStatusAsync(request), this, true);
}
