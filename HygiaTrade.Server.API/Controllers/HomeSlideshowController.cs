using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/home-slideshow")]
public class HomeSlideshowController(
    IHomeSlideshowService homeSlideshowService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<HomeSlideshowPayload>> GetAsync()
    {
        HomeSlideshowPayload payload =
            await homeSlideshowService.GetAsync();

        return Ok(payload);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut]
    public async Task<ActionResult<HomeSlideshowPayload>> UpdateAsync(
        [FromBody] HomeSlideshowPayload payload)
    {
        try
        {
            HomeSlideshowPayload updated =
                await homeSlideshowService.UpdateAsync(payload);

            return Ok(updated);
        }
        catch (HomeSlideshowServiceException exception)
        {
            return StatusCode(
                exception.StatusCode,
                new { message = exception.Message });
        }
    }
}

public sealed class HomeSlideshowPayload
{
    public List<HomeSlideDto> Slides { get; set; } = [];
}

public sealed class HomeSlideDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
    public string EyebrowBg { get; set; } = string.Empty;
    public string EyebrowEn { get; set; } = string.Empty;
    public string TitleBg { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string BadgeBg { get; set; } = string.Empty;
    public string BadgeEn { get; set; } = string.Empty;
    public string NoteBg { get; set; } = string.Empty;
    public string NoteEn { get; set; } = string.Empty;
    public string CtaBg { get; set; } = string.Empty;
    public string CtaEn { get; set; } = string.Empty;
    public string CtaUrl { get; set; } = "/products";
    public string Image { get; set; } = string.Empty;
    public string Accent { get; set; } = "from-teal-100 via-cyan-50 to-white";
}
