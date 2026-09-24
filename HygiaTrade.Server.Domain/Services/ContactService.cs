using HygiaTrade.Common.Requests.Contact;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.Domain.Services;

public sealed class ContactService(
    IEmailNotificationService emailNotificationService) : IContactService
{
    public async Task SendAsync(CreateContactRequest request)
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
    }
}
