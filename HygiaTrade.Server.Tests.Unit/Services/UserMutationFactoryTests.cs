using HygiaTrade.Common.Requests.Users;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class UserMutationFactoryTests
{
    private readonly UserMutationFactory factory = new();

    [Fact]
    public void CreateCurrentUserUpdate_MapsResolvedIdAndEditableFields()
    {
        Guid userId = Guid.NewGuid();

        UpdateCurrentUserRequest request = new()
        {
            Email = "new@example.com",
            Names = "New Name",
            Phone = "999"
        };

        UpdateUserRequest result =
            factory.CreateCurrentUserUpdate(
                userId,
                request);

        Assert.Equal(userId, result.Id);
        Assert.Equal(request.Email, result.Email);
        Assert.Equal(request.Names, result.Names);
        Assert.Equal(request.Phone, result.Phone);
    }

    [Fact]
    public void CreateProfileUpdate_ReplacesEditableFieldsAndPreservesIdentitySecurityAndRole()
    {
        DateTime createdOn =
            new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        DateTime modifiedOn =
            new(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        DateTime expiry =
            new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        User existing = new()
        {
            CreatedOn = createdOn,
            ModifiedOn = modifiedOn,
            Email = "old@example.com",
            Names = "Old",
            Phone = "111",
            PasswordHash = "hash",
            Role = "Admin",
            RefreshToken = "token",
            RefreshTokenExpiryTime = expiry
        };

        UpdateUserRequest request = new()
        {
            Id = existing.Id,
            Email = "new@example.com",
            Names = "New",
            Phone = "222"
        };

        User result =
            factory.CreateProfileUpdate(
                existing,
                request);

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(createdOn, result.CreatedOn);
        Assert.Equal(modifiedOn, result.ModifiedOn);
        Assert.Equal("new@example.com", result.Email);
        Assert.Equal("New", result.Names);
        Assert.Equal("222", result.Phone);
        Assert.Equal("hash", result.PasswordHash);
        Assert.Equal("Admin", result.Role);
        Assert.Equal("token", result.RefreshToken);
        Assert.Equal(expiry, result.RefreshTokenExpiryTime);
    }

    [Fact]
    public void CreateRoleUpdate_ChangesOnlyRoleAndPreservesUserData()
    {
        User existing = new()
        {
            Email = "user@example.com",
            Names = "User",
            Phone = "123",
            PasswordHash = "hash",
            Role = "RegisteredCustomer",
            RefreshToken = "token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1)
        };

        User result =
            factory.CreateRoleUpdate(
                existing,
                "Admin");

        Assert.Equal(existing.Id, result.Id);
        Assert.Equal(existing.CreatedOn, result.CreatedOn);
        Assert.Equal(existing.ModifiedOn, result.ModifiedOn);
        Assert.Equal(existing.Email, result.Email);
        Assert.Equal(existing.Names, result.Names);
        Assert.Equal(existing.Phone, result.Phone);
        Assert.Equal(existing.PasswordHash, result.PasswordHash);
        Assert.Equal("Admin", result.Role);
        Assert.Equal(existing.RefreshToken, result.RefreshToken);
        Assert.Equal(
            existing.RefreshTokenExpiryTime,
            result.RefreshTokenExpiryTime);
    }
}
