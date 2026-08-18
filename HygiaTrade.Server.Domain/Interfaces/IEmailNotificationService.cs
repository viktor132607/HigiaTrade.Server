using HygiaTrade.Data.Entities;

namespace HygiaTrade.Domain.Interfaces;

public interface IEmailNotificationService
{
	Task SendPasswordResetAsync(User user, string resetLink);

	Task SendOrderConfirmationAsync(
		User user,
		Order order,
		string paymentMethod,
		string deliveryMethod);

	Task SendOrderStatusChangedAsync(User user, Order order);

	Task SendContactMessageAsync(
		string name,
		string email,
		string? phone,
		string subject,
		string message);
}
