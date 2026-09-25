using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Data.Interfaces;
using HygiaTrade.Domain.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class GdprCurrentUserResolverTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCurrentUserIdAsync_Throws401_WhenCurrentUserIdIsMissing(
        string? currentUserId)
    {
        var auth = new Mock<IAuthService>();
        var users = new Mock<IUserRepository>();

        auth
            .Setup(x => x.GetCurrentUserId())
            .ReturnsAsync(currentUserId);

        var resolver =
            new GdprCurrentUserResolver(
                auth.Object,
                users.Object);

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => resolver.GetCurrentUserIdAsync());

        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("Unauthorized", ex.Message);

        users.Verify(
            x => x.GetByIdAsync(
                It.IsAny<Guid>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_PreservesGuidParseFailure()
    {
        var auth = new Mock<IAuthService>();

        auth
            .Setup(x => x.GetCurrentUserId())
            .ReturnsAsync("not-a-guid");

        var resolver =
            new GdprCurrentUserResolver(
                auth.Object,
                Mock.Of<IUserRepository>());

        await Assert.ThrowsAsync<FormatException>(
            () => resolver.GetCurrentUserIdAsync());
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_Throws404_WhenRepositoryUserIsMissing()
    {
        Guid userId = Guid.NewGuid();

        var auth = new Mock<IAuthService>();
        var users = new Mock<IUserRepository>();

        auth
            .Setup(x => x.GetCurrentUserId())
            .ReturnsAsync(userId.ToString());

        users
            .Setup(x => x.GetByIdAsync(userId))
            .Returns(
                new ValueTask<User?>(
                    (User?)null));

        var resolver =
            new GdprCurrentUserResolver(
                auth.Object,
                users.Object);

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => resolver.GetCurrentUserIdAsync());

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("User not found.", ex.Message);
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_ReturnsParsedId_WhenUserExists()
    {
        Guid userId = Guid.NewGuid();

        User user = new()
        {
            Id = userId,
            Email = "user@example.com",
            Names = "User",
            Phone = "123",
            PasswordHash = "hash"
        };

        var auth = new Mock<IAuthService>();
        var users = new Mock<IUserRepository>();

        auth
            .Setup(x => x.GetCurrentUserId())
            .ReturnsAsync(userId.ToString());

        users
            .Setup(x => x.GetByIdAsync(userId))
            .Returns(
                new ValueTask<User?>(user));

        var resolver =
            new GdprCurrentUserResolver(
                auth.Object,
                users.Object);

        Guid result =
            await resolver.GetCurrentUserIdAsync();

        Assert.Equal(userId, result);
    }
}
