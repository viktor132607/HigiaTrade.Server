using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using HygiaTrade.Common.Requests.Contact;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController(
	IEmailNotificationService emailNotificationService) : ControllerBase
{
	[AllowAnonymous]
	[HttpPost]
	[EnableRateLimiting("contact")]
	public async Task<IActionResult> SendAsync(
		[FromBody] CreateContactRequest request)
	{
		string subject = string.IsNullOrWhiteSpace(request.Subject)
			? "Contact enquiry"
			: request.Subject.Trim();

		await emailNotificationService.SendContactMessageAsync(
			request.Name.Trim(),
			request.Email.Trim(),
			request.Phone.Trim(),
			subject,
			request.Message.Trim());

		return Ok(new
		{
			message = "Contact message sent successfully."
		});
	}
}
