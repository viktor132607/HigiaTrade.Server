using HygiaTrade.Core.Exceptions;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public interface IUserCurrentUserResolver
{
    Task<Guid> GetCurrentUserIdAsync();
}

public sealed class UserCurrentUserResolver(
    IAuthService authService)
    : IUserCurrentUserResolver
{
    public async Task<Guid> GetCurrentUserIdAsync()
    {
        string? currentUserId =
            await authService.GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw new AppException("Unauthorized")
                .SetStatusCode(401);
        }

        return Guid.Parse(currentUserId);
    }
}
