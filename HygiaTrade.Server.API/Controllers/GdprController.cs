using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HygiaTrade.Common.Responses.Gdpr;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GdprController(IGdprService gdprService) : ControllerBase
{
    [HttpGet("export")]
    public async Task<ActionResult<GdprExportResponse>> ExportData()
    {
        GdprExportResponse response = await gdprService.ExportCurrentUserDataAsync();
        return Ok(response);
    }

    [HttpDelete("delete-account")]
    public async Task<ActionResult<GdprDeleteResponse>> DeleteAccount()
    {
        GdprDeleteResponse response = await gdprService.DeleteCurrentUserDataAsync();
        return Ok(response);
    }
}
