using HygiaTrade.Common.Requests.Users;
using HygiaTrade.Common.Responses.Users;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public class UserService(IUserRepository userRepository, IAuthService authService) : IUserService
{
    public async Task<IEnumerable<UserResponse>?> GetAsync()
    {
        IEnumerable<User> users = (await userRepository.GetAllAsync())
            .OrderByDescending(user => user.CreatedOn);

        return users.Select(MapUser);
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id)
    {
        User? user = await userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new AppException("User not found.").SetStatusCode(404);
        }

        return MapUser(user);
    }

    public async Task<UserResponse?> GetCurrentUserAsync()
    {
        string? currentUserId = await authService.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw new AppException("Unauthorized").SetStatusCode(401);
        }

        return await GetByIdAsync(Guid.Parse(currentUserId));
    }

    public async Task<UserResponse?> UpdateCurrentUserAsync(UpdateCurrentUserRequest request)
    {
        string? currentUserId = await authService.GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            throw new AppException("Unauthorized").SetStatusCode(401);
        }

        UpdateUserRequest updateRequest = new()
        {
            Id = Guid.Parse(currentUserId),
            Email = request.Email,
            Names = request.Names,
            Phone = request.Phone
        };

        return await UpdateAsync(updateRequest);
    }

    public async Task<UserResponse?> UpdateAsync(UpdateUserRequest request)
    {
        User? userBeforeUpdate = await userRepository.GetByIdAsync(request.Id);
        if (userBeforeUpdate == null)
        {
            throw new AppException("User not found.").SetStatusCode(404);
        }

        User updatedUserPayload = new()
        {
            Id = request.Id,
            CreatedOn = userBeforeUpdate.CreatedOn,
            ModifiedOn = userBeforeUpdate.ModifiedOn,
            Email = request.Email,
            Names = request.Names,
            Phone = request.Phone,
            PasswordHash = userBeforeUpdate.PasswordHash,
            Role = userBeforeUpdate.Role,
            RefreshToken = userBeforeUpdate.RefreshToken,
            RefreshTokenExpiryTime = userBeforeUpdate.RefreshTokenExpiryTime
        };

        User? updatedUser = await userRepository.UpdateAsync(updatedUserPayload);
        if (updatedUser == null)
        {
            throw new AppException("User not found.").SetStatusCode(404);
        }

        return MapUser(updatedUser);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        UserResponse user = (await GetByIdAsync(id))!;

        if (!await userRepository.DeleteAsync(user.Id))
        {
            return false;
        }

        return true;
    }

    public Task<bool> PromoteToAdminAsync(RoleChangeRequest request)
    {
        return ChangeRoleAsync(request, Roles.Admin);
    }

    public Task<bool> DemoteToRegisteredCustomerAsync(RoleChangeRequest request)
    {
        return ChangeRoleAsync(request, Roles.RegisteredCustomer);
    }

    private async Task<bool> ChangeRoleAsync(RoleChangeRequest request, string toRole)
    {
        User? userBeforeUpdate = await userRepository.GetByIdAsync(request.UserId);

        if (userBeforeUpdate == null)
        {
            throw new AppException("User not found.").SetStatusCode(404);
        }

        User updatedUserPayload = new()
        {
            Id = request.UserId,
            CreatedOn = userBeforeUpdate.CreatedOn,
            ModifiedOn = userBeforeUpdate.ModifiedOn,
            Email = userBeforeUpdate.Email,
            Names = userBeforeUpdate.Names,
            Phone = userBeforeUpdate.Phone,
            PasswordHash = userBeforeUpdate.PasswordHash,
            Role = toRole,
            RefreshToken = userBeforeUpdate.RefreshToken,
            RefreshTokenExpiryTime = userBeforeUpdate.RefreshTokenExpiryTime
        };

        User? updatedUser = await userRepository.UpdateAsync(updatedUserPayload);
        return updatedUser != null;
    }

    private static UserResponse MapUser(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            Names = user.Names,
            Phone = user.Phone,
            Role = user.Role,
        };
    }
}