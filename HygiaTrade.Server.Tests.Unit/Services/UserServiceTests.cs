using HygiaTrade.Common.Requests.Users;
using HygiaTrade.Common.Responses.Users;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class UserServiceTests
{
    private readonly Mock<IUserRepository> repository = new();
    private readonly Mock<IUserCurrentUserResolver> resolver = new();
    private readonly Mock<IUserMapper> mapper = new();
    private readonly Mock<IUserMutationFactory> mutationFactory = new();

    private UserService CreateService() =>
        new(
            repository.Object,
            resolver.Object,
            mapper.Object,
            mutationFactory.Object);

    [Fact]
    public async Task GetAsync_ReturnsUsersNewestFirstAndMapsEachUser()
    {
        User older = UserFor("older@example.com");
        older = CopyWithCreatedOn(
            older,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        User newer = UserFor("newer@example.com");
        newer = CopyWithCreatedOn(
            newer,
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));

        repository
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync([older, newer]);

        UserResponse olderResponse =
            ResponseFor(older);

        UserResponse newerResponse =
            ResponseFor(newer);

        mapper
            .Setup(x => x.ToResponse(older))
            .Returns(olderResponse);

        mapper
            .Setup(x => x.ToResponse(newer))
            .Returns(newerResponse);

        IEnumerable<UserResponse>? result =
            await CreateService().GetAsync();

        Assert.NotNull(result);

        Assert.Equal(
            [newerResponse, olderResponse],
            result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedUser()
    {
        User user = UserFor("user@example.com");
        UserResponse expected = ResponseFor(user);

        repository
            .Setup(x => x.GetByIdAsync(user.Id))
            .Returns(new ValueTask<User?>(user));

        mapper
            .Setup(x => x.ToResponse(user))
            .Returns(expected);

        UserResponse? result =
            await CreateService().GetByIdAsync(user.Id);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetByIdAsync_Throws404_WhenUserDoesNotExist()
    {
        Guid id = Guid.NewGuid();

        repository
            .Setup(x => x.GetByIdAsync(id))
            .Returns(new ValueTask<User?>((User?)null));

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().GetByIdAsync(id));

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("User not found.", ex.Message);
    }

    [Fact]
    public async Task GetCurrentUserAsync_UsesResolvedUserId()
    {
        User user = UserFor("user@example.com");
        UserResponse expected = ResponseFor(user);

        resolver
            .Setup(x => x.GetCurrentUserIdAsync())
            .ReturnsAsync(user.Id);

        repository
            .Setup(x => x.GetByIdAsync(user.Id))
            .Returns(new ValueTask<User?>(user));

        mapper
            .Setup(x => x.ToResponse(user))
            .Returns(expected);

        UserResponse? result =
            await CreateService().GetCurrentUserAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task UpdateCurrentUserAsync_BuildsCurrentUserRequestAndUsesUpdateFlow()
    {
        User existing = UserFor("old@example.com");

        UpdateCurrentUserRequest request = new()
        {
            Email = "new@example.com",
            Names = "New Name",
            Phone = "999"
        };

        UpdateUserRequest updateRequest = new()
        {
            Id = existing.Id,
            Email = request.Email,
            Names = request.Names,
            Phone = request.Phone
        };

        User updatePayload =
            UserFor(request.Email);

        updatePayload.Names = request.Names;
        updatePayload.Phone = request.Phone;

        User updated =
            UserFor(request.Email);

        updated.Names = request.Names;
        updated.Phone = request.Phone;

        UserResponse expected = ResponseFor(updated);

        resolver
            .Setup(x => x.GetCurrentUserIdAsync())
            .ReturnsAsync(existing.Id);

        mutationFactory
            .Setup(x => x.CreateCurrentUserUpdate(
                existing.Id,
                request))
            .Returns(updateRequest);

        repository
            .Setup(x => x.GetByIdAsync(existing.Id))
            .Returns(new ValueTask<User?>(existing));

        mutationFactory
            .Setup(x => x.CreateProfileUpdate(
                existing,
                updateRequest))
            .Returns(updatePayload);

        repository
            .Setup(x => x.UpdateAsync(updatePayload))
            .Returns(new ValueTask<User?>(updated));

        mapper
            .Setup(x => x.ToResponse(updated))
            .Returns(expected);

        UserResponse? result =
            await CreateService()
                .UpdateCurrentUserAsync(request);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task UpdateAsync_Throws404_WhenRepositoryUpdateReturnsNull()
    {
        User existing = UserFor("old@example.com");

        UpdateUserRequest request = new()
        {
            Id = existing.Id,
            Email = "new@example.com",
            Names = "New",
            Phone = "123"
        };

        User payload = UserFor("new@example.com");

        repository
            .Setup(x => x.GetByIdAsync(existing.Id))
            .Returns(new ValueTask<User?>(existing));

        mutationFactory
            .Setup(x => x.CreateProfileUpdate(
                existing,
                request))
            .Returns(payload);

        repository
            .Setup(x => x.UpdateAsync(payload))
            .Returns(new ValueTask<User?>((User?)null));

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService().UpdateAsync(request));

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("User not found.", ex.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteAsync_ReturnsRepositoryResult(
        bool deleteResult)
    {
        User existing = UserFor("user@example.com");

        repository
            .Setup(x => x.GetByIdAsync(existing.Id))
            .Returns(new ValueTask<User?>(existing));

        repository
            .Setup(x => x.DeleteAsync(existing.Id))
            .ReturnsAsync(deleteResult);

        bool result =
            await CreateService().DeleteAsync(existing.Id);

        Assert.Equal(deleteResult, result);
    }

    [Fact]
    public async Task DeleteAsync_Throws404BeforeDelete_WhenUserDoesNotExist()
    {
        Guid id = Guid.NewGuid();

        repository
            .Setup(x => x.GetByIdAsync(id))
            .Returns(new ValueTask<User?>((User?)null));

        await Assert.ThrowsAsync<AppException>(
            () => CreateService().DeleteAsync(id));

        repository.Verify(
            x => x.DeleteAsync(
                It.IsAny<Guid>()),
            Times.Never);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("RegisteredCustomer")]
    public async Task RoleChange_UsesExpectedRoleAndReturnsRepositoryResult(
        string role)
    {
        User existing = UserFor("user@example.com");
        RoleChangeRequest request = new()
        {
            UserId = existing.Id
        };

        User payload = UserFor(existing.Email);
        payload.Role = role;

        repository
            .Setup(x => x.GetByIdAsync(existing.Id))
            .Returns(new ValueTask<User?>(existing));

        mutationFactory
            .Setup(x => x.CreateRoleUpdate(
                existing,
                role))
            .Returns(payload);

        repository
            .Setup(x => x.UpdateAsync(payload))
            .Returns(new ValueTask<User?>(payload));

        bool result =
            role == "Admin"
                ? await CreateService().PromoteToAdminAsync(request)
                : await CreateService().DemoteToRegisteredCustomerAsync(request);

        Assert.True(result);
    }

    [Fact]
    public async Task RoleChange_ReturnsFalse_WhenRepositoryUpdateReturnsNull()
    {
        User existing = UserFor("user@example.com");
        RoleChangeRequest request = new()
        {
            UserId = existing.Id
        };

        User payload = UserFor(existing.Email);
        payload.Role = "Admin";

        repository
            .Setup(x => x.GetByIdAsync(existing.Id))
            .Returns(new ValueTask<User?>(existing));

        mutationFactory
            .Setup(x => x.CreateRoleUpdate(
                existing,
                "Admin"))
            .Returns(payload);

        repository
            .Setup(x => x.UpdateAsync(payload))
            .Returns(new ValueTask<User?>((User?)null));

        bool result =
            await CreateService().PromoteToAdminAsync(request);

        Assert.False(result);
    }

    private static User UserFor(string email) =>
        new()
        {
            Email = email,
            Names = "User",
            Phone = "123",
            PasswordHash = "hash"
        };

    private static User CopyWithCreatedOn(
        User source,
        DateTime createdOn) =>
        new()
        {
            Id = source.Id,
            CreatedOn = createdOn,
            ModifiedOn = source.ModifiedOn,
            Email = source.Email,
            Names = source.Names,
            Phone = source.Phone,
            PasswordHash = source.PasswordHash,
            Role = source.Role,
            RefreshToken = source.RefreshToken,
            RefreshTokenExpiryTime =
                source.RefreshTokenExpiryTime
        };

    private static UserResponse ResponseFor(User user) =>
        new()
        {
            Id = user.Id,
            Email = user.Email,
            Names = user.Names,
            Phone = user.Phone,
            Role = user.Role
        };
}
