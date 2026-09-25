using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Services;

public sealed class ResendEmailNotificationService(
	IEmailNotificationTemplateBuilder templateBuilder,
	IResendEmailTransport transport) : IEmailNotificationService
{
	public Task SendPasswordResetAsync(
		User user,
		string resetLink) =>
		transport.SendAsync(
			templateBuilder.BuildPasswordReset(
				user,
				resetLink));

	public Task SendOrderConfirmationAsync(
		User user,
		Order order,
		string paymentMethod,
		string deliveryMethod) =>
		transport.SendAsync(
			templateBuilder.BuildOrderConfirmation(
				user,
				order,
				paymentMethod,
				deliveryMethod));

	public Task SendOrderStatusChangedAsync(
		User user,
		Order order) =>
		transport.SendAsync(
			templateBuilder.BuildOrderStatusChanged(
				user,
				order));

	public Task SendContactMessageAsync(
		string name,
		string email,
		string? phone,
		string subject,
		string message) =>
		transport.SendAsync(
			templateBuilder.BuildContactMessage(
				name,
				email,
				phone,
				subject,
				message));
}
