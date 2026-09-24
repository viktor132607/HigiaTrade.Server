using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController(
    IStoredImageService imageService) : ControllerBase
{
    private const long MaxImageSize = 10 * 1024 * 1024;

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("upload")]
    [RequestSizeLimit(MaxImageSize)]
    public async Task<IActionResult> UploadAsync(
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            StoredImageUploadResult image =
                await imageService.UploadAsync(
                    file,
                    cancellationToken);

            string url =
                $"{Request.Scheme}://{Request.Host}/api/Images/{image.Id}";

            return Ok(new
            {
                id = image.Id,
                url,
                fileName = image.FileName,
                contentType = image.ContentType,
                size = image.Size
            });
        }
        catch (StoredImageServiceException exception)
        {
            return StatusCode(
                exception.StatusCode,
                new { message = exception.Message });
        }
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        StoredImageDownloadResult? image =
            await imageService.GetAsync(
                id,
                cancellationToken);

        if (image is null)
        {
            return NotFound();
        }

        return File(
            image.Data,
            image.ContentType);
    }
}
