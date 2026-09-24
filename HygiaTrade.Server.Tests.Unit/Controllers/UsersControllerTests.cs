using HygiaTrade.API.Controllers;
using HygiaTrade.Common.Requests.Auth;
using HygiaTrade.Common.Requests.Users;
using HygiaTrade.Common.Responses.Auth;
using HygiaTrade.Common.Responses.Users;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class UsersControllerTests
{
    private readonly Mock<IUserService> userService = new();
    private readonly Mock<IAuthService> authService = new();

    private UsersController CreateController() =>
        new(userService.Object, authService.Object);

    [Fact]
    public async Task GetAllAsync_ReturnsOk()
    {
        IEnumerable<UserResponse> expected =
        [
            CreateUser()
        ];

        userService
            .Setup(service => service.GetAsync())
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().GetAllAsync();

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsOk()
    {
        Guid id = Guid.NewGuid();
        UserResponse expected = CreateUser(id);

        userService
            .Setup(service => service.GetByIdAsync(id))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().GetByIdAsync(id);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task CreateAsync_ReturnsOk()
    {
        RegisterUserRequest request = new()
        {
            Email = "user@example.com",
            Password = "Password123!",
            Names = "Test User",
            Phone = "0888000000"
        };

        RegisterUserResponse expected = new()
        {
            Id = Guid.NewGuid()
        };

        authService
            .Setup(service => service.RegisterAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().CreateAsync(request);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsOk()
    {
        UpdateUserRequest request = new()
        {
            Id = Guid.NewGuid(),
            Email = "updated@example.com",
            Names = "Updated",
            Phone = "0888111111"
        };

        UserResponse expected =
            CreateUser(request.Id);

        userService
            .Setup(service => service.UpdateAsync(request))
            .ReturnsAsync(expected);

        IActionResult result =
            await CreateController().UpdateAsync(request);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsOk_WhenDeleted()
    {
        Guid id = Guid.NewGuid();

        userService
            .Setup(service => service.DeleteAsync(id))
            .ReturnsAsync(true);

        IActionResult result =
            await CreateController().DeleteAsync(id);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task PromoteToAdmin_ReturnsOk_WhenServiceSucceeds()
    {
        RoleChangeRequest request = new()
        {
            UserId = Guid.NewGuid()
        };

        userService
            .Setup(service => service.PromoteToAdminAsync(request))
            .ReturnsAsync(true);

        IActionResult result =
            await CreateController().PromoteToAdmin(request);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task DemoteToRegisteredCustomer_ReturnsOk_WhenServiceSucceeds()
    {
        RoleChangeRequest request = new()
        {
            UserId = Guid.NewGuid()
        };

        userService
            .Setup(service =>
                service.DemoteToRegisteredCustomerAsync(request))
            .ReturnsAsync(true);

        IActionResult result =
            await CreateController()
                .DemoteToRegisteredCustomer(request);

        Assert.IsType<OkResult>(result);
    }

    private static UserResponse CreateUser(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = "user@example.com",
        Names = "Test User",
        Phone = "0888000000"
    };
}
