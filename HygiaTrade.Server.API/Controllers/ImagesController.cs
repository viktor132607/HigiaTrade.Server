using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HygiaTrade.Core.StaticClasses;
using HygiaTrade.Data;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImagesController(ApplicationDbContext db) : ControllerBase
{
    private const long MaxImageSize = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    ];

    [Authorize(Roles = Roles.Admin)]
    [HttpPost("upload")]
    [RequestSizeLimit(MaxImageSize)]
    public async Task<IActionResult> UploadAsync([FromForm] IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest(new { message = "Choose an image to upload." });
        }

        if (file.Length > MaxImageSize)
        {
            return BadRequest(new { message = "Image size cannot exceed 10 MB." });
        }

        string contentType = file.ContentType.ToLowerInvariant();
        if (!AllowedContentTypes.Contains(contentType))
        {
            return BadRequest(new { message = "Only JPEG, PNG, WEBP and GIF images are supported." });
        }

        await using MemoryStream stream = new();
        await file.CopyToAsync(stream);

        StoredImage storedImage = new()
        {
            FileName = Path.GetFileName(file.FileName),
            ContentType = contentType,
            Data = stream.ToArray()
        };

        db.StoredImages.Add(storedImage);
        await db.SaveChangesAsync();

        string url = $"{Request.Scheme}://{Request.Host}/api/Images/{storedImage.Id}";

        return Ok(new
        {
            id = storedImage.Id,
            url,
            fileName = storedImage.FileName,
            contentType = storedImage.ContentType,
            size = storedImage.Data.Length
        });
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id)
    {
        StoredImage? image = await db.StoredImages
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

        if (image is null)
        {
            return NotFound();
        }

        return File(image.Data, image.ContentType);
    }
}
