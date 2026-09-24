using HygiaTrade.Common.Requests.Contact;

namespace HygiaTrade.Domain.Interfaces;

public interface IContactService
{
    Task SendAsync(CreateContactRequest request);
}
