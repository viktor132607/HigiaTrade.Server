using HygiaTrade.API.Controllers;
using HygiaTrade.Common.Requests.Wishlist;
using HygiaTrade.Common.Responses.Wishlist;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class WishlistControllerTests
{
    private readonly Mock<IWishlistService> wishlistService = new();

    private WishlistController CreateController() =>
        new(wishlistService.Object);

    [Fact]
    public async Task GetAsync_ReturnsOk()
    {
        WishlistResponse expected = new();

        wishlistService
            .Setup(service => service.GetByJWT())
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().GetAsync();

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task AddProductToWhishlistAsync_ReturnsOk_WhenServiceSucceeds()
    {
        AddToWishlistRequest request = new()
        {
            ProductId = Guid.NewGuid()
        };

        wishlistService
            .Setup(service =>
                service.AddProductToWishlistAsync(request.ProductId))
            .ReturnsAsync(true);

        IActionResult result =
            await CreateController()
                .AddProductToWhishlistAsync(request);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RemoveProductFromWhishlistAsync_ReturnsOk_WhenServiceSucceeds()
    {
        RemoveFromWishlistRequest request = new()
        {
            ProductId = Guid.NewGuid()
        };

        wishlistService
            .Setup(service =>
                service.RemoveProductFromWishlistAsync(request.ProductId))
            .ReturnsAsync(true);

        IActionResult result =
            await CreateController()
                .RemoveProductFromWhishlistAsync(request);

        Assert.IsType<OkResult>(result);
    }
}
