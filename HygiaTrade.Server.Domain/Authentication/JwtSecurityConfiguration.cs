using System.Text;
using Microsoft.IdentityModel.Tokens;
using HygiaTrade.Common.Options;

namespace HygiaTrade.Domain.Authentication;

public static class JwtSecurityConfiguration
{
    private const int HmacSha512MinimumSecretLengthInBytes = 64;

    public const string SigningAlgorithm = SecurityAlgorithms.HmacSha512;

    public static void ValidateOrThrow(JwtOptions? jwtOptions)
    {
        IReadOnlyCollection<string> validationErrors = Validate(jwtOptions);
        if (validationErrors.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Invalid JWT configuration. {string.Join(" ", validationErrors)}");
    }

    public static IReadOnlyCollection<string> Validate(JwtOptions? jwtOptions)
    {
        List<string> validationErrors = new();

        if (jwtOptions is null)
        {
            validationErrors.Add("The 'Jwt' configuration section is missing.");
            return validationErrors;
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Issuer))
        {
            validationErrors.Add("The 'Jwt:Issuer' value is required.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Audience))
        {
            validationErrors.Add("The 'Jwt:Audience' value is required.");
        }

        if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
        {
            validationErrors.Add("The 'Jwt:Secret' value is required.");
        }
        else
        {
            byte[] secretBytes = Encoding.UTF8.GetBytes(jwtOptions.Secret);
            if (secretBytes.Length <= HmacSha512MinimumSecretLengthInBytes)
            {
                validationErrors.Add(
                    $"The 'Jwt:Secret' value must be longer than {HmacSha512MinimumSecretLengthInBytes} bytes (more than {HmacSha512MinimumSecretLengthInBytes * 8} bits) for {SigningAlgorithm}. Current size: {secretBytes.Length} bytes ({secretBytes.Length * 8} bits). Generate a valid secret with 'openssl rand -base64 64' and set it as 'Jwt__Secret' in your host configuration.");
            }
        }

        if (jwtOptions.AccessTokenExpiryMinutes <= 0)
        {
            validationErrors.Add("The 'Jwt:AccessTokenExpiryMinutes' value must be greater than 0.");
        }

        if (jwtOptions.RefreshTokenExpiryDays <= 0)
        {
            validationErrors.Add("The 'Jwt:RefreshTokenExpiryDays' value must be greater than 0.");
        }

        return validationErrors;
    }

    public static SigningCredentials CreateSigningCredentials(JwtOptions jwtOptions)
    {
        SymmetricSecurityKey signingKey = CreateSigningKey(jwtOptions);
        return new SigningCredentials(signingKey, SigningAlgorithm);
    }

    public static SymmetricSecurityKey CreateSigningKey(JwtOptions jwtOptions)
    {
        ValidateOrThrow(jwtOptions);

        byte[] secretBytes = Encoding.UTF8.GetBytes(jwtOptions.Secret);
        return new SymmetricSecurityKey(secretBytes);
    }

    public static TokenValidationParameters CreateTokenValidationParameters(JwtOptions jwtOptions)
    {
        SymmetricSecurityKey signingKey = CreateSigningKey(jwtOptions);

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = signingKey,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = new[] { SigningAlgorithm }
        };
    }
}
