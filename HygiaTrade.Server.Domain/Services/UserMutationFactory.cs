using HygiaTrade.Common.Requests.Users;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.Domain.Services;

public interface IUserMutationFactory
{
    UpdateUserRequest CreateCurrentUserUpdate(
        Guid userId,
        UpdateCurrentUserRequest request);

    User CreateProfileUpdate(
        User existingUser,
        UpdateUserRequest request);

    User CreateRoleUpdate(
        User existingUser,
        string role);
}

public sealed class UserMutationFactory
    : IUserMutationFactory
{
    public UpdateUserRequest CreateCurrentUserUpdate(
        Guid userId,
        UpdateCurrentUserRequest request) =>
        new()
        {
            Id = userId,
            Email = request.Email,
            Names = request.Names,
            Phone = request.Phone
        };

    public User CreateProfileUpdate(
        User existingUser,
        UpdateUserRequest request) =>
        CopyExisting(
            existingUser,
            request.Email,
            request.Names,
            request.Phone,
            existingUser.Role);

    public User CreateRoleUpdate(
        User existingUser,
        string role) =>
        CopyExisting(
            existingUser,
            existingUser.Email,
            existingUser.Names,
            existingUser.Phone,
            role);

    private static User CopyExisting(
        User existingUser,
        string email,
        string names,
        string phone,
        string? role) =>
        new()
        {
            Id = existingUser.Id,
            CreatedOn = existingUser.CreatedOn,
            ModifiedOn = existingUser.ModifiedOn,
            Email = email,
            Names = names,
            Phone = phone,
            PasswordHash = existingUser.PasswordHash,
            Role = role,
            RefreshToken = existingUser.RefreshToken,
            RefreshTokenExpiryTime =
                existingUser.RefreshTokenExpiryTime
        };
}
