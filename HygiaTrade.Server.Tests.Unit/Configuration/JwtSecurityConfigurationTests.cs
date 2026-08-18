using System;
using Microsoft.Extensions.Options;
using HygiaTrade.Common.Options;
using HygiaTrade.Domain.Authentication;
using Xunit;

namespace HygiaTrade.Tests.Unit.Configuration;

public class JwtSecurityConfigurationTests
{
    [Fact]
    public void ValidateOrThrow_ShouldThrowClearError_WhenSecretIsTooShortForHs512()
    {
        JwtOptions jwtOptions = new()
        {
            Issuer = "HygiaTrade.Tests",
            Audience = "HygiaTrade.Tests.Client",
            Secret = "ThisSecretIsTooShortForHs512Signing",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 30
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => JwtSecurityConfiguration.ValidateOrThrow(jwtOptions));

        Assert.Contains("Jwt:Secret", exception.Message);
        Assert.Contains("longer than 64 bytes", exception.Message);
        Assert.Contains("openssl rand -base64 64", exception.Message);
        Assert.Contains(JwtSecurityConfiguration.SigningAlgorithm, exception.Message);
    }

    [Fact]
    public void JwtOptionsValidator_ShouldReturnFailure_WhenSecretIsTooShortForHs512()
    {
        JwtOptionsValidator validator = new();
        JwtOptions jwtOptions = new()
        {
            Issuer = "HygiaTrade.Tests",
            Audience = "HygiaTrade.Tests.Client",
            Secret = "ThisSecretIsTooShortForHs512Signing",
            AccessTokenExpiryMinutes = 60,
            RefreshTokenExpiryDays = 30
        };

        ValidateOptionsResult result = validator.Validate(null, jwtOptions);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failures);
        Assert.Contains(result.Failures!, failure => failure.Contains("Jwt:Secret"));
    }
}
