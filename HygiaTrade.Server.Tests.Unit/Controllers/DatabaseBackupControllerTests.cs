using HygiaTrade.API.Controllers;
using HygiaTrade.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class DatabaseBackupControllerTests
{
    private readonly Mock<IDatabaseBackupService> backupService = new();
    private readonly Mock<ILogger<DatabaseBackupController>> logger = new();

    private DatabaseBackupController CreateController() =>
        new(backupService.Object, logger.Object);

    [Fact]
    public async Task ExportAsync_ReturnsFile_WhenBackupIsCreated()
    {
        string path = Path.GetTempFileName();
        await File.WriteAllBytesAsync(path, [1, 2, 3, 4]);

        DatabaseBackupArtifact artifact =
            new(path, "backup.dump");

        backupService
            .Setup(service => service.CreateBackupAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        IActionResult result =
            await CreateController().ExportAsync(
                CancellationToken.None);

        FileStreamResult file =
            Assert.IsType<FileStreamResult>(result);

        Assert.Equal(
            "application/octet-stream",
            file.ContentType);

        Assert.Equal(
            artifact.FileName,
            file.FileDownloadName);

        Assert.False(file.EnableRangeProcessing);

        Assert.IsType<FileStream>(file.FileStream);

        await file.FileStream.DisposeAsync();

        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task ExportAsync_Returns500_WhenBackupServiceFails()
    {
        DatabaseBackupException exception =
            new("Backup failed.");

        backupService
            .Setup(service => service.CreateBackupAsync(
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().ExportAsync(
                CancellationToken.None);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            error.StatusCode);

        Assert.Equal(
            exception.Message,
            GetProperty(error.Value, "message"));
    }

    [Fact]
    public async Task ExportAsync_Rethrows_WhenBackupFileCannotBeOpened()
    {
        string missingPath = Path.Combine(
            Path.GetTempPath(),
            $"{Guid.NewGuid():N}.dump");

        DatabaseBackupArtifact artifact =
            new(missingPath, "missing.dump");

        backupService
            .Setup(service => service.CreateBackupAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => CreateController().ExportAsync(
                CancellationToken.None));
    }


    [Fact]
    public async Task ExportAsync_Rethrows_WhenBackupPathIsDirectory_AndCleanupAlsoFails()
    {
        string directoryPath = Path.Combine(
            Path.GetTempPath(),
            $"backup-dir-{Guid.NewGuid():N}");

        Directory.CreateDirectory(directoryPath);

        try
        {
            DatabaseBackupArtifact artifact =
                new(directoryPath, "backup.dump");

            backupService
                .Setup(service => service.CreateBackupAsync(
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(artifact);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => CreateController().ExportAsync(
                    CancellationToken.None));
        }
        finally
        {
            Directory.Delete(directoryPath);
        }
    }

    [Fact]
    public async Task RestoreAsync_ReturnsBadRequest_WhenArchiveIsNull()
    {
        IActionResult result =
            await CreateController().RestoreAsync(
                null,
                CancellationToken.None);

        BadRequestObjectResult badRequest =
            Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            "A non-empty PostgreSQL backup archive is required.",
            GetProperty(badRequest.Value, "message"));

        backupService.Verify(
            service => service.RestoreBackupAsync(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RestoreAsync_ReturnsBadRequest_WhenArchiveIsEmpty()
    {
        IFormFile archive =
            CreateFormFile([], "empty.dump");

        IActionResult result =
            await CreateController().RestoreAsync(
                archive,
                CancellationToken.None);

        BadRequestObjectResult badRequest =
            Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            "A non-empty PostgreSQL backup archive is required.",
            GetProperty(badRequest.Value, "message"));

        backupService.Verify(
            service => service.RestoreBackupAsync(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RestoreAsync_ReturnsBadRequest_WhenArchiveIsInvalid()
    {
        IFormFile archive =
            CreateFormFile([1, 2, 3], "invalid.dump");

        InvalidDataException exception =
            new("Invalid backup archive.");

        backupService
            .Setup(service => service.RestoreBackupAsync(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().RestoreAsync(
                archive,
                CancellationToken.None);

        BadRequestObjectResult badRequest =
            Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            exception.Message,
            GetProperty(badRequest.Value, "message"));
    }

    [Fact]
    public async Task RestoreAsync_Returns500_WhenBackupServiceFails()
    {
        IFormFile archive =
            CreateFormFile([1, 2, 3], "backup.dump");

        DatabaseBackupException exception =
            new("Restore failed.");

        backupService
            .Setup(service => service.RestoreBackupAsync(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        IActionResult result =
            await CreateController().RestoreAsync(
                archive,
                CancellationToken.None);

        ObjectResult error =
            Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            error.StatusCode);

        Assert.Equal(
            exception.Message,
            GetProperty(error.Value, "message"));
    }

    [Fact]
    public async Task RestoreAsync_ReturnsOk_WhenRestoreSucceeds()
    {
        IFormFile archive =
            CreateFormFile([1, 2, 3, 4], "../backup.dump");

        Stream? passedStream = null;

        backupService
            .Setup(service => service.RestoreBackupAsync(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .Callback<Stream, CancellationToken>(
                (stream, _) => passedStream = stream)
            .Returns(Task.CompletedTask);

        IActionResult result =
            await CreateController().RestoreAsync(
                archive,
                CancellationToken.None);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            "Database restored successfully.",
            GetProperty(ok.Value, "message"));

        object? restoredAt =
            GetProperty(ok.Value, "restoredAtUtc");

        Assert.IsType<DateTime>(restoredAt);

        backupService.Verify(
            service => service.RestoreBackupAsync(
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.NotNull(passedStream);
    }

    private static IFormFile CreateFormFile(
        byte[] data,
        string fileName)
    {
        MemoryStream stream = new(data);

        return new FormFile(
            stream,
            0,
            data.Length,
            "archive",
            fileName);
    }

    private static object? GetProperty(
        object? value,
        string name)
    {
        Assert.NotNull(value);

        return value
            .GetType()
            .GetProperty(name)
            ?.GetValue(value);
    }
}
