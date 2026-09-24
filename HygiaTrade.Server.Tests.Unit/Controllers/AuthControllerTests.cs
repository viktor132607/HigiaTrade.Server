using HygiaTrade.API.Controllers;
using HygiaTrade.Common.Requests.Auth;
using HygiaTrade.Common.Requests.Users;
using HygiaTrade.Common.Responses.Auth;
using HygiaTrade.Common.Responses.Users;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class AuthControllerTests
{
    private readonly Mock<IAuthService> authService = new();
    private readonly Mock<IUserService> userService = new();

    private AuthController CreateController() =>
        new(authService.Object, userService.Object);

    [Fact]
    public async Task Register_ReturnsOk_WithServiceResult()
    {
        RegisterUserRequest request = CreateRegisterRequest();
        RegisterUserResponse expected = new() { Id = Guid.NewGuid() };

        authService
            .Setup(service => service.RegisterAsync(request))
            .ReturnsAsync(expected);

        ActionResult<RegisterUserResponse> result =
            await CreateController().Register(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenCredentialsAreInvalid()
    {
        LoginUserRequest request = CreateLoginRequest();

        authService
            .Setup(service => service.LoginAsync(request))
            .ReturnsAsync((TokenResponse?)null);

        ActionResult<TokenResponse> result =
            await CreateController().Login(request);

        BadRequestObjectResult badRequest =
            Assert.IsType<BadRequestObjectResult>(result.Result);

        Assert.Equal(
            "Invalid username or password.",
            badRequest.Value);
    }

    [Fact]
    public async Task Login_ReturnsOk_WhenServiceReturnsToken()
    {
        LoginUserRequest request = CreateLoginRequest();
        TokenResponse expected = CreateTokenResponse();

        authService
            .Setup(service => service.LoginAsync(request))
            .ReturnsAsync(expected);

        ActionResult<TokenResponse> result =
            await CreateController().Login(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task ForgotPassword_ReturnsOk_WithServiceResult()
    {
        ForgotPasswordRequest request = new()
        {
            Email = "user@example.com"
        };

        ForgotPasswordResponse expected = new()
        {
            Message = "sent"
        };

        authService
            .Setup(service => service.ForgotPasswordAsync(request))
            .ReturnsAsync(expected);

        ActionResult<ForgotPasswordResponse> result =
            await CreateController().ForgotPassword(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task ResetPassword_ReturnsOk_WhenServiceSucceeds()
    {
        ResetPasswordRequest request = CreateResetPasswordRequest();

        authService
            .Setup(service => service.ResetPasswordAsync(request))
            .ReturnsAsync(true);

        IActionResult result =
            await CreateController().ResetPassword(request);

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task ResetPassword_ReturnsBadRequest_WhenServiceFails()
    {
        ResetPasswordRequest request = CreateResetPasswordRequest();

        authService
            .Setup(service => service.ResetPasswordAsync(request))
            .ReturnsAsync(false);

        IActionResult result =
            await CreateController().ResetPassword(request);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Logout_ReturnsUnauthorized_WhenServiceFails()
    {
        authService
            .Setup(service => service.LogoutAsync())
            .ReturnsAsync(false);

        IActionResult result =
            await CreateController().Logout();

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Logout_ReturnsOk_WhenServiceSucceeds()
    {
        authService
            .Setup(service => service.LogoutAsync())
            .ReturnsAsync(true);

        IActionResult result =
            await CreateController().Logout();

        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task RefreshToken_ReturnsUnauthorized_WhenResultIsNull()
    {
        RefreshTokenRequest request = CreateRefreshTokenRequest();

        authService
            .Setup(service => service.RefreshTokensAsync(request))
            .ReturnsAsync((TokenResponse?)null);

        ActionResult<TokenResponse> result =
            await CreateController().RefreshToken(request);

        UnauthorizedObjectResult unauthorized =
            Assert.IsType<UnauthorizedObjectResult>(result.Result);

        Assert.Equal(
            "Invalid refresh token.",
            unauthorized.Value);
    }

    [Fact]
    public async Task RefreshToken_ReturnsUnauthorized_WhenAccessTokenIsNull()
    {
        RefreshTokenRequest request = CreateRefreshTokenRequest();
        TokenResponse token = new()
        {
            AccessToken = null!,
            RefreshToken = "refresh-token"
        };

        authService
            .Setup(service => service.RefreshTokensAsync(request))
            .ReturnsAsync(token);

        ActionResult<TokenResponse> result =
            await CreateController().RefreshToken(request);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task RefreshToken_ReturnsUnauthorized_WhenRefreshTokenIsNull()
    {
        RefreshTokenRequest request = CreateRefreshTokenRequest();
        TokenResponse token = new()
        {
            AccessToken = "access-token",
            RefreshToken = null!
        };

        authService
            .Setup(service => service.RefreshTokensAsync(request))
            .ReturnsAsync(token);

        ActionResult<TokenResponse> result =
            await CreateController().RefreshToken(request);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task RefreshToken_ReturnsOk_WhenBothTokensExist()
    {
        RefreshTokenRequest request = CreateRefreshTokenRequest();
        TokenResponse expected = CreateTokenResponse();

        authService
            .Setup(service => service.RefreshTokensAsync(request))
            .ReturnsAsync(expected);

        ActionResult<TokenResponse> result =
            await CreateController().RefreshToken(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsUnauthorized_WhenUserIsMissing()
    {
        userService
            .Setup(service => service.GetCurrentUserAsync())
            .ReturnsAsync((UserResponse?)null);

        ActionResult<UserResponse> result =
            await CreateController().GetCurrentUser();

        UnauthorizedObjectResult unauthorized =
            Assert.IsType<UnauthorizedObjectResult>(result.Result);

        Assert.Equal(
            "Current user was not found.",
            unauthorized.Value);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsOk_WhenUserExists()
    {
        UserResponse expected = CreateUserResponse();

        userService
            .Setup(service => service.GetCurrentUserAsync())
            .ReturnsAsync(expected);

        ActionResult<UserResponse> result =
            await CreateController().GetCurrentUser();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task UpdateCurrentUser_ReturnsUnauthorized_WhenUserIsMissing()
    {
        UpdateCurrentUserRequest request =
            CreateUpdateCurrentUserRequest();

        userService
            .Setup(service => service.UpdateCurrentUserAsync(request))
            .ReturnsAsync((UserResponse?)null);

        ActionResult<UserResponse> result =
            await CreateController().UpdateCurrentUser(request);

        UnauthorizedObjectResult unauthorized =
            Assert.IsType<UnauthorizedObjectResult>(result.Result);

        Assert.Equal(
            "Current user was not found.",
            unauthorized.Value);
    }

    [Fact]
    public async Task UpdateCurrentUser_ReturnsOk_WhenUserExists()
    {
        UpdateCurrentUserRequest request =
            CreateUpdateCurrentUserRequest();

        UserResponse expected = CreateUserResponse();

        userService
            .Setup(service => service.UpdateCurrentUserAsync(request))
            .ReturnsAsync(expected);

        ActionResult<UserResponse> result =
            await CreateController().UpdateCurrentUser(request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public void AuthenticatedOnlyEndpoint_ReturnsOk()
    {
        IActionResult result =
            CreateController().AuthenticatedOnlyEndpoint();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            "You are authenticated!",
            ok.Value);
    }

    [Fact]
    public void AdminOnlyEndpoint_ReturnsOk()
    {
        IActionResult result =
            CreateController().AdminOnlyEndpoint();

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            "You are an admin!",
            ok.Value);
    }

    private static RegisterUserRequest CreateRegisterRequest() => new()
    {
        Email = "user@example.com",
        Password = "Password123!",
        Names = "Test User",
        Phone = "0888000000"
    };

    private static LoginUserRequest CreateLoginRequest() => new()
    {
        Email = "user@example.com",
        Password = "Password123!"
    };

    private static ResetPasswordRequest CreateResetPasswordRequest() => new()
    {
        Token = "reset-token",
        NewPassword = "NewPassword123!"
    };

    private static RefreshTokenRequest CreateRefreshTokenRequest() => new()
    {
        UserId = Guid.NewGuid(),
        RefreshToken = "refresh-token"
    };

    private static UpdateCurrentUserRequest CreateUpdateCurrentUserRequest() =>
        new()
        {
            Email = "user@example.com",
            Names = "Updated User",
            Phone = "0888111111"
        };

    private static TokenResponse CreateTokenResponse() => new()
    {
        AccessToken = "access-token",
        RefreshToken = "refresh-token"
    };

    private static UserResponse CreateUserResponse() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        Names = "Test User",
        Phone = "0888000000"
    };
}
