using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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

namespace HygiaTrade.Domain.Services;

public class AuthService(
    ApplicationDbContext context,
    IUserRepository userRepository,
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtOptions> jwtOptions,
    IOptions<ClientAppOptions> clientAppOptions,
    IOptions<EmailOptions> emailOptions,
    IPasswordResetTokenStore passwordResetTokenStore,
    IEmailNotificationService emailNotificationService) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly ClientAppOptions _clientAppOptions = clientAppOptions.Value;
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    public async Task<RegisterUserResponse?> RegisterAsync(RegisterUserRequest request)
    {
        if (await userRepository.IsEmailAlreadyUsed(request.Email))
        {
            throw new AppException("Email is already in use.").SetStatusCode(409);
        }

        User user = new()
        {
            Email = request.Email,
            PasswordHash = "temporaryPasswordHash",
            Names = request.Names,
            Phone = request.Phone,
            Role = Roles.RegisteredCustomer
        };

        string hashedPassword = new PasswordHasher<User>().HashPassword(user, request.Password);

        user.PasswordHash = hashedPassword;

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return new RegisterUserResponse
        {
            Id = user.Id,
        };
    }

    public async Task<TokenResponse?> LoginAsync(LoginUserRequest request)
    {
        User? user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

        if (user is null)
        {
            return null;
        }

        PasswordVerificationResult passwordResult = new PasswordHasher<User>()
            .VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return await CreateTokenResponse(user);
    }

    public async Task<TokenResponse?> RefreshTokensAsync(RefreshTokenRequest request)
    {
        User? user = await ValidateRefreshTokenAsync(request.UserId, request.RefreshToken);

        if (user is null)
        {
            return null;
        }

        return await CreateTokenResponse(user);
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        User? user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && !u.IsDeleted);

        ForgotPasswordResponse response = new()
        {
            Message = "If an account with this email exists, a password reset link has been prepared."
        };

        if (user is null)
        {
            return response;
        }

        TimeSpan expiresIn = TimeSpan.FromMinutes(_emailOptions.PasswordResetTokenExpiryMinutes);
        string token = passwordResetTokenStore.CreateToken(user.Id, expiresIn);
        string resetLink = $"{_clientAppOptions.BaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";

        await emailNotificationService.SendPasswordResetAsync(user, resetLink);

        if (_emailOptions.DeliveryMode.Equals("Console", StringComparison.OrdinalIgnoreCase))
        {
            response.PreviewResetLink = resetLink;
        }

        return response;
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        Guid? userId = passwordResetTokenStore.ConsumeToken(request.Token);

        if (userId is null)
        {
            throw new AppException("The password reset link is invalid or expired.").SetStatusCode(400);
        }

        User? user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted);

        if (user is null)
        {
            throw new AppException("User not found.").SetStatusCode(404);
        }

        string hashedPassword = new PasswordHasher<User>().HashPassword(user, request.NewPassword);

        user.PasswordHash = hashedPassword;
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> LogoutAsync()
    {
        string? currentUserId = await GetCurrentUserId();

        if (!Guid.TryParse(currentUserId, out Guid userId))
        {
            throw new AppException("Invalid or missing user id claim.").SetStatusCode(401);
        }

        User? user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user is null)
        {
            throw new AppException("User not found.").SetStatusCode(404);
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;

        await context.SaveChangesAsync();

        return true;
    }

    private async Task<User?> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
    {
        User? user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return null;
        }

        return user;
    }

    private async Task<TokenResponse> CreateTokenResponse(User user)
    {
        return new TokenResponse
        {
            AccessToken = CreateToken(user),
            RefreshToken = await GenerateAndSaveRefreshTokenAsync(user),
            UserId = user.Id,
            Email = user.Email,
            Names = user.Names,
            Phone = user.Phone,
            Role = user.Role
        };
    }

    private string GenerateRefreshToken()
    {
        byte[] randomNumber = new byte[32];

        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return Convert.ToBase64String(randomNumber);
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(User user)
    {
        string refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays);

        await context.SaveChangesAsync();

        return refreshToken;
    }

    private string CreateToken(User user)
    {
        string role = user.Role ?? Roles.RegisteredCustomer;
        string userId = user.Id.ToString();

        List<Claim> claims =
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim("id", userId),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role)
        ];

        SigningCredentials creds = JwtSecurityConfiguration.CreateSigningCredentials(_jwtOptions);

        JwtSecurityToken tokenDescriptor = new(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiryMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }

    public Task<string?> GetCurrentUserId()
    {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;

        string? value =
            user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
            user?.FindFirst("sub")?.Value ??
            user?.FindFirst("id")?.Value;

        return Task.FromResult(value);
    }

    public Task<string?> GetCurrentUserEmail()
    {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;

        string? value =
            user?.FindFirst(ClaimTypes.Name)?.Value ??
            user?.FindFirst(JwtRegisteredClaimNames.Email)?.Value ??
            user?.FindFirst("email")?.Value;

        return Task.FromResult(value);
    }

    public Task<string?> GetCurrentUserRole()
    {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;

        string? value =
            user?.FindFirst(ClaimTypes.Role)?.Value ??
            user?.FindFirst("role")?.Value;

        return Task.FromResult(value);
    }
}
