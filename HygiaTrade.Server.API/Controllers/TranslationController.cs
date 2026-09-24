using HygiaTrade.API.Services;
using HygiaTrade.Core.StaticClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/translation")]
public sealed class TranslationController(
    ITranslationService translationService) : ControllerBase
{
    [HttpPost("bg-to-en")]
    public async Task<ActionResult<TranslationResponse>> TranslateBgToEnAsync(
        [FromBody] TranslationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            TranslationResponse response =
                await translationService.TranslateBgToEnAsync(
                    request,
                    cancellationToken);

            return Ok(response);
        }
        catch (TranslationServiceException exception)
        {
            return StatusCode(
                exception.StatusCode,
                exception.Payload);
        }
    }
}

public sealed class TranslationRequest
{
    public string? Text { get; set; }
}

public sealed class TranslationResponse
{
    public string Translation { get; set; } = string.Empty;
}
