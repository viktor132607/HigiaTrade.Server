using HygiaTrade.Core.Exceptions;
using HygiaTrade.Domain.Interfaces;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class UserCurrentUserResolverTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetCurrentUserIdAsync_Throws401_WhenIdIsMissing(
        string? currentUserId)
    {
        var auth = new Mock<IAuthService>();

        auth
            .Setup(x => x.GetCurrentUserId())
            .ReturnsAsync(currentUserId);

        var resolver =
            new UserCurrentUserResolver(
                auth.Object);

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => resolver.GetCurrentUserIdAsync());

        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("Unauthorized", ex.Message);
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_PreservesGuidParseFailure()
    {
        var auth = new Mock<IAuthService>();

        auth
            .Setup(x => x.GetCurrentUserId())
            .ReturnsAsync("invalid-guid");

        var resolver =
            new UserCurrentUserResolver(
                auth.Object);

        await Assert.ThrowsAsync<FormatException>(
            () => resolver.GetCurrentUserIdAsync());
    }

    [Fact]
    public async Task GetCurrentUserIdAsync_ReturnsParsedGuid()
    {
        Guid expected = Guid.NewGuid();

        var auth = new Mock<IAuthService>();

        auth
            .Setup(x => x.GetCurrentUserId())
            .ReturnsAsync(expected.ToString());

        var resolver =
            new UserCurrentUserResolver(
                auth.Object);

        Guid result =
            await resolver.GetCurrentUserIdAsync();

        Assert.Equal(expected, result);
    }
}
