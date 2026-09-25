using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public interface IGdprCurrentUserResolver
{
    Task<Guid> GetCurrentUserIdAsync();
}

public sealed class GdprCurrentUserResolver(
    IAuthService authService,
    IUserRepository userRepository)
    : IGdprCurrentUserResolver
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

        Guid userId = Guid.Parse(currentUserId);

        User? user =
            await userRepository.GetByIdAsync(userId);

        if (user is null)
        {
            throw GdprErrors.UserNotFound();
        }

        return userId;
    }
}
