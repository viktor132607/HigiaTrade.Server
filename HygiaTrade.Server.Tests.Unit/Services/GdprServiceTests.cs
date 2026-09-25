using HygiaTrade.Common.Responses.Gdpr;
using HygiaTrade.Core.Exceptions;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Services;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class GdprServiceTests
{
    private readonly Mock<IGdprCurrentUserResolver> resolver = new();
    private readonly Mock<IGdprRepository> repository = new();
    private readonly Mock<IGdprExportMapper> mapper = new();
    private readonly Mock<IGdprDataAnonymizer> anonymizer = new();
    private readonly Mock<IGdprClock> clock = new();

    private GdprService CreateService() =>
        new(
            resolver.Object,
            repository.Object,
            mapper.Object,
            anonymizer.Object,
            clock.Object);

    [Fact]
    public async Task ExportCurrentUserDataAsync_MapsResolvedUserData()
    {
        Guid userId = Guid.NewGuid();
        User user = UserFor(userId);
        var data = new GdprExportData(user, [], [], []);
        DateTime requestedAt =
            new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);

        GdprExportResponse expected = new()
        {
            RequestedAtUtc = requestedAt
        };

        resolver
            .Setup(x => x.GetCurrentUserIdAsync())
            .ReturnsAsync(userId);

        repository
            .Setup(x => x.GetExportDataAsync(userId))
            .ReturnsAsync(data);

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(requestedAt);

        mapper
            .Setup(x => x.Map(data, requestedAt))
            .Returns(expected);

        GdprExportResponse result =
            await CreateService().ExportCurrentUserDataAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task ExportCurrentUserDataAsync_Throws404_WhenRepositoryUserIsMissing()
    {
        Guid userId = Guid.NewGuid();

        resolver
            .Setup(x => x.GetCurrentUserIdAsync())
            .ReturnsAsync(userId);

        repository
            .Setup(x => x.GetExportDataAsync(userId))
            .ReturnsAsync((GdprExportData?)null);

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService()
                    .ExportCurrentUserDataAsync());

        Assert.Equal(404, ex.StatusCode);
        Assert.Equal("User not found.", ex.Message);

        mapper.Verify(
            x => x.Map(
                It.IsAny<GdprExportData>(),
                It.IsAny<DateTime>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteCurrentUserDataAsync_AnonymizesSavesAndReturnsConfirmation()
    {
        Guid userId = Guid.NewGuid();
        User user = UserFor(userId);
        var data = new GdprDeletionData(user, [], [], []);

        resolver
            .Setup(x => x.GetCurrentUserIdAsync())
            .ReturnsAsync(userId);

        repository
            .Setup(x => x.GetDeletionDataAsync(userId))
            .ReturnsAsync(data);

        GdprDeleteResponse result =
            await CreateService().DeleteCurrentUserDataAsync();

        Assert.True(result.Deleted);
        Assert.Equal(
            "Your personal account data has been anonymized and marked for deletion.",
            result.Message);

        anonymizer.Verify(
            x => x.Anonymize(data),
            Times.Once);

        repository.Verify(
            x => x.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task DeleteCurrentUserDataAsync_Throws404_WithoutMutatingOrSaving_WhenRepositoryUserIsMissing()
    {
        Guid userId = Guid.NewGuid();

        resolver
            .Setup(x => x.GetCurrentUserIdAsync())
            .ReturnsAsync(userId);

        repository
            .Setup(x => x.GetDeletionDataAsync(userId))
            .ReturnsAsync((GdprDeletionData?)null);

        AppException ex =
            await Assert.ThrowsAsync<AppException>(
                () => CreateService()
                    .DeleteCurrentUserDataAsync());

        Assert.Equal(404, ex.StatusCode);

        anonymizer.Verify(
            x => x.Anonymize(
                It.IsAny<GdprDeletionData>()),
            Times.Never);

        repository.Verify(
            x => x.SaveChangesAsync(),
            Times.Never);
    }

    private static User UserFor(Guid id) =>
        new()
        {
            Id = id,
            Email = "user@example.com",
            Names = "User",
            Phone = "123",
            PasswordHash = "hash"
        };
}
