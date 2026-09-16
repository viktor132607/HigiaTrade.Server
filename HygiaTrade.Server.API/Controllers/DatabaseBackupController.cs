using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/database-backup")]
public sealed class DatabaseBackupController(
    DatabaseBackupService databaseBackupService,
    ILogger<DatabaseBackupController> logger) : ControllerBase
{
    [HttpGet("export")]
    [Produces("application/octet-stream")]
    public async Task<IActionResult> ExportAsync(CancellationToken cancellationToken)
    {
        DatabaseBackupArtifact backup;

        try
        {
            backup = await databaseBackupService.CreateBackupAsync(cancellationToken);
        }
        catch (DatabaseBackupException ex)
        {
            logger.LogError(ex, "Full database backup export failed.");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = ex.Message });
        }

        try
        {
            FileStream stream = new(
                backup.FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                128 * 1024,
                FileOptions.Asynchronous |
                FileOptions.SequentialScan |
                FileOptions.DeleteOnClose);

            return File(
                stream,
                "application/octet-stream",
                backup.FileName,
                enableRangeProcessing: false);
        }
        catch
        {
            try
            {
                System.IO.File.Delete(backup.FilePath);
            }
            catch
            {
                // Best-effort cleanup only.
            }

            throw;
        }
    }

    [HttpPost("restore")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<IActionResult> RestoreAsync(
        [FromForm] IFormFile? archive,
        CancellationToken cancellationToken)
    {
        if (archive is null || archive.Length == 0)
        {
            return BadRequest(new
            {
                message = "A non-empty PostgreSQL backup archive is required."
            });
        }

        try
        {
            await using Stream archiveStream = archive.OpenReadStream();
            await databaseBackupService.RestoreBackupAsync(
                archiveStream,
                cancellationToken);
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DatabaseBackupException ex)
        {
            logger.LogError(ex, "Full database restore failed.");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = ex.Message });
        }

        logger.LogWarning(
            "An administrator restored the full database from uploaded archive {ArchiveName} ({ArchiveSize} bytes).",
            Path.GetFileName(archive.FileName),
            archive.Length);

        return Ok(new
        {
            message = "Database restored successfully.",
            restoredAtUtc = DateTime.UtcNow
        });
    }
}
