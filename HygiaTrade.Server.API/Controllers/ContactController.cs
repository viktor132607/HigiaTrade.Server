using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HygiaTrade.Common.Requests.Contact;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController(
    IContactService contactService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("contact")]
    public async Task<IActionResult> SendAsync(
        [FromBody] CreateContactRequest request)
    {
        await contactService.SendAsync(request);

        return Ok(new
        {
            message = "Contact message sent successfully."
        });
    }
}
