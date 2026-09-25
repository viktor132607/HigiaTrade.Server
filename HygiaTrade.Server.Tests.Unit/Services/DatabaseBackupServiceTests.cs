using HygiaTrade.API.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class DatabaseBackupServiceTests
{
    private readonly Mock<IPostgresBackupTool> tool = new();
    private readonly Mock<IDatabaseBackupArchiveValidator> validator = new();
    private readonly Mock<IDatabaseBackupFileStore> files = new();
    private readonly Mock<IDatabaseBackupOperationLock> operationLock = new();
    private readonly Mock<ILogger<DatabaseBackupService>> logger = new();

    public DatabaseBackupServiceTests()
    {
        operationLock
            .Setup(x => x.AcquireAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TestLease());
    }

    private DatabaseBackupService CreateService() =>
        new(
            tool.Object,
            validator.Object,
            files.Object,
            operationLock.Object,
            logger.Object);

    [Fact]
    public async Task CreateBackupAsync_ReturnsArtifact_WhenBackupIsValid()
    {
        const string path = "/tmp/backup.dump";

        files.Setup(x => x.CreateTemporaryPath("dump"))
            .Returns(path);

        files.Setup(x => x.GetLength(path))
            .Returns(42);

        DatabaseBackupArtifact result =
            await CreateService().CreateBackupAsync();

        Assert.Equal(path, result.FilePath);
        Assert.StartsWith(
            "higiatrade-full-database-",
            result.FileName);
        Assert.EndsWith("Z.dump", result.FileName);

        tool.Verify(
            x => x.CreateBackupAsync(
                path,
                It.IsAny<CancellationToken>()),
            Times.Once);

        validator.Verify(
            x => x.ValidateHeaderAsync(
                path,
                It.IsAny<CancellationToken>()),
            Times.Once);

        files.Verify(
            x => x.TryDelete(path),
            Times.Never);
    }

    [Fact]
    public async Task CreateBackupAsync_WrapsInvalidGeneratedArchiveAndCleansUp()
    {
        const string path = "/tmp/invalid.dump";

        files.Setup(x => x.CreateTemporaryPath("dump"))
            .Returns(path);

        validator
            .Setup(x => x.ValidateHeaderAsync(
                path,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("bad"));

        DatabaseBackupException exception =
            await Assert.ThrowsAsync<DatabaseBackupException>(
                () => CreateService().CreateBackupAsync());

        Assert.Equal(
            "The generated PostgreSQL backup archive failed validation.",
            exception.Message);

        Assert.IsType<InvalidDataException>(
            exception.InnerException);

        files.Verify(
            x => x.TryDelete(path),
            Times.Once);
    }

    [Fact]
    public async Task CreateBackupAsync_RejectsEmptyGeneratedArchiveAndCleansUp()
    {
        const string path = "/tmp/empty.dump";

        files.Setup(x => x.CreateTemporaryPath("dump"))
            .Returns(path);

        files.Setup(x => x.GetLength(path))
            .Returns(0);

        DatabaseBackupException exception =
            await Assert.ThrowsAsync<DatabaseBackupException>(
                () => CreateService().CreateBackupAsync());

        Assert.Contains("archive is empty", exception.Message);

        files.Verify(
            x => x.TryDelete(path),
            Times.Once);
    }

    [Fact]
    public async Task CreateBackupAsync_PropagatesToolFailureAndCleansUp()
    {
        const string path = "/tmp/fail.dump";
        var expected =
            new DatabaseBackupException("pg_dump failed");

        files.Setup(x => x.CreateTemporaryPath("dump"))
            .Returns(path);

        tool.Setup(x => x.CreateBackupAsync(
                path,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expected);

        DatabaseBackupException actual =
            await Assert.ThrowsAsync<DatabaseBackupException>(
                () => CreateService().CreateBackupAsync());

        Assert.Same(expected, actual);

        files.Verify(
            x => x.TryDelete(path),
            Times.Once);
    }

    [Fact]
    public async Task RestoreBackupAsync_RejectsNullStreamBeforeLock()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => CreateService()
                .RestoreBackupAsync(null!));

        operationLock.Verify(
            x => x.AcquireAsync(
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RestoreBackupAsync_RejectsEmptyUploadAndCleansUp()
    {
        const string path = "/tmp/upload.dump";

        files.Setup(x => x.CreateTemporaryPath("dump"))
            .Returns(path);

        files.Setup(x => x.GetLength(path))
            .Returns(0);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => CreateService().RestoreBackupAsync(
                new MemoryStream([1])));

        tool.Verify(
            x => x.ValidateArchiveAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        files.Verify(
            x => x.TryDelete(path),
            Times.Once);
    }

    [Fact]
    public async Task RestoreBackupAsync_RejectsInvalidHeaderAndCleansUp()
    {
        const string path = "/tmp/upload.dump";

        files.Setup(x => x.CreateTemporaryPath("dump"))
            .Returns(path);

        files.Setup(x => x.GetLength(path))
            .Returns(10);

        validator.Setup(x => x.ValidateHeaderAsync(
                path,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("invalid"));

        await Assert.ThrowsAsync<InvalidDataException>(
            () => CreateService().RestoreBackupAsync(
                new MemoryStream([1])));

        files.Verify(
            x => x.TryDelete(path),
            Times.Once);
    }

    [Fact]
    public async Task RestoreBackupAsync_ConvertsToolValidationFailureToInvalidData()
    {
        const string path = "/tmp/upload.dump";

        files.Setup(x => x.CreateTemporaryPath("dump"))
            .Returns(path);

        files.Setup(x => x.GetLength(path))
            .Returns(10);

        tool.Setup(x => x.ValidateArchiveAsync(
                path,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new DatabaseBackupException(
                    "pg_restore failed"));

        InvalidDataException exception =
            await Assert.ThrowsAsync<InvalidDataException>(
                () => CreateService().RestoreBackupAsync(
                    new MemoryStream([1])));

        Assert.Equal(
            "The uploaded file is not a valid PostgreSQL custom-format backup archive.",
            exception.Message);

        Assert.IsType<DatabaseBackupException>(
            exception.InnerException);

        files.Verify(
            x => x.TryDelete(path),
            Times.Once);
    }

    [Fact]
    public async Task RestoreBackupAsync_ValidatesRestoresAndAlwaysDeletesUpload()
    {
        const string path = "/tmp/upload.dump";

        files.Setup(x => x.CreateTemporaryPath("dump"))
            .Returns(path);

        files.Setup(x => x.GetLength(path))
            .Returns(100);

        await CreateService().RestoreBackupAsync(
            new MemoryStream([1, 2, 3]));

        files.Verify(
            x => x.CopyToFileAsync(
                It.IsAny<Stream>(),
                path,
                It.IsAny<CancellationToken>()),
            Times.Once);

        validator.Verify(
            x => x.ValidateHeaderAsync(
                path,
                It.IsAny<CancellationToken>()),
            Times.Once);

        tool.Verify(
            x => x.ValidateArchiveAsync(
                path,
                It.IsAny<CancellationToken>()),
            Times.Once);

        tool.Verify(
            x => x.RestoreBackupAsync(
                path,
                It.IsAny<CancellationToken>()),
            Times.Once);

        files.Verify(
            x => x.TryDelete(path),
            Times.Once);
    }

    private sealed class TestLease : IAsyncDisposable
    {
        public ValueTask DisposeAsync() =>
            ValueTask.CompletedTask;
    }
}
