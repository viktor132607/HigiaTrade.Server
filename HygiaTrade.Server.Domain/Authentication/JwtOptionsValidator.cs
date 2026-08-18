using Microsoft.Extensions.Options;
using HygiaTrade.Common.Options;

namespace HygiaTrade.Domain.Authentication;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        IReadOnlyCollection<string> validationErrors = JwtSecurityConfiguration.Validate(options);
        if (validationErrors.Count == 0)
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(validationErrors);
    }
}
