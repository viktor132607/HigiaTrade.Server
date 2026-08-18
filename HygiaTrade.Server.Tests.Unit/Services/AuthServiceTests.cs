using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using HygiaTrade.Common.Options;
using HygiaTrade.Common.Requests.Auth;
using HygiaTrade.Common.Responses.Auth;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Authentication;
using HygiaTrade.Domain.Interfaces;
using HygiaTrade.Domain.Services;
using Xunit;

namespace HygiaTrade.Tests.Unit.Services;

public class AuthServiceTests
{
    private readonly AuthService _authService;
    private readonly ApplicationDbContext _context;
    private readonly JwtOptions _jwtOptions;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IPasswordResetTokenStore> _passwordResetTokenStoreMock;
    private readonly Mock<IEmailNotificationService> _emailNotificationServiceMock;

    public AuthServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb-{Guid.NewGuid()}")
            .Options;
        _context = new(options);
        _userRepositoryMock = new();
        _httpContextAccessorMock = new();
        _passwordResetTokenStoreMock = new();
        _emailNotificationServiceMock = new();
        _jwtOptions = new JwtOptions
        {
            Issuer = "HygiaTrade.Tests",
            Audience = "HygiaTrade.Tests.Client",
            Secret = "TestJwtSecretValueThatExceedsSixtyFourBytesForHs512SigningAndValidation123456789",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 30
        };
        _authService = new(
            _context,
            _userRepositoryMock.Object,
            _httpContextAccessorMock.Object,
            Options.Create(_jwtOptions),
            Options.Create(new ClientAppOptions
            {
                BaseUrl = "http://localhost:5173"
            }),
            Options.Create(new EmailOptions()),
            _passwordResetTokenStoreMock.Object,
            _emailNotificationServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowAppException_WhenEmailIsAlreadyUsed()
    {
        _userRepositoryMock.Setup(x => x.IsEmailAlreadyUsed(It.IsAny<string>())).ReturnsAsync(true);

        RegisterUserRequest request = new()
        {
            Email = "test@example.com",
            Password = "password123",
            Names = "Test User",
            Phone = "123456789"
        };

        await Assert.ThrowsAsync<AppException>(() => _authService.RegisterAsync(request));
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnCorrectResponse_WhenUserIsSuccessfullyRegistered()
    {
        _userRepositoryMock.Setup(x => x.IsEmailAlreadyUsed(It.IsAny<string>())).ReturnsAsync(false);

        RegisterUserRequest request = new()
        {
            Email = "test@example.com",
            Password = "password123",
            Names = "Test User",
            Phone = "123456789"
        };

        RegisterUserResponse? response = await _authService.RegisterAsync(request);

        Assert.NotNull(response);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_WhenUserNotFound()
    {
        _userRepositoryMock.Setup(x => x.IsEmailAlreadyUsed(It.IsAny<string>())).ReturnsAsync(false);

        LoginUserRequest request = new() { Email = "nonexistent@example.com", Password = "password123" };

        TokenResponse? response = await _authService.LoginAsync(request);

        Assert.Null(response);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokenAcceptedByJwtValidation_WhenCredentialsAreValid()
    {
        User user = new()
        {
            Email = "customer@example.com",
            Names = "Customer Example",
            Phone = "123456789",
            Role = Roles.RegisteredCustomer,
            PasswordHash = "temporaryPasswordHash"
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "password123");

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        LoginUserRequest request = new()
        {
            Email = "customer@example.com",
            Password = "password123"
        };

        TokenResponse? response = await _authService.LoginAsync(request);

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));

        JwtSecurityTokenHandler tokenHandler = new();
        TokenValidationParameters tokenValidationParameters = JwtSecurityConfiguration.CreateTokenValidationParameters(_jwtOptions);
        ClaimsPrincipal claimsPrincipal = tokenHandler.ValidateToken(
            response.AccessToken,
            tokenValidationParameters,
            out SecurityToken validatedToken);
        JwtSecurityToken jwtSecurityToken = Assert.IsType<JwtSecurityToken>(validatedToken);

        Assert.Equal(JwtSecurityConfiguration.SigningAlgorithm, jwtSecurityToken.Header.Alg);
        Assert.Equal(user.Email, claimsPrincipal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal(user.Id.ToString(), claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(user.Role, claimsPrincipal.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public async Task LogoutAsync_ShouldReturnTrue_WhenLogoutIsSuccessful()
    {
        _context.Users.Add(new()
        {
            RefreshToken = "refreshToken",
            Email = "test",
            Names = "test",
            Phone = "test",
            PasswordHash = "test"

        });
        await _context.SaveChangesAsync();
        
        _httpContextAccessorMock.Setup(x => x.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)).Returns(new Claim(ClaimTypes.NameIdentifier, _context.Users.First().Id.ToString()));

        bool result = await _authService.LogoutAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task RefreshTokensAsync_ShouldReturnNull_WhenUserNotFound()
    {
        RefreshTokenRequest request = new() { UserId = Guid.NewGuid(), RefreshToken = "invalidRefreshToken" };

        TokenResponse? response = await _authService.RefreshTokensAsync(request);

        Assert.Null(response);
    }
}
