using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HygiaTrade.Common.Options;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Services;

public class ConsoleEmailNotificationService(
	ILogger<ConsoleEmailNotificationService> logger,
	IOptions<PaymentOptions> paymentOptions) : IEmailNotificationService
{
	private readonly PaymentOptions _paymentOptions = paymentOptions.Value;

	public Task SendPasswordResetAsync(User user, string resetLink)
	{
		logger.LogInformation(
			"Password reset requested for {Email}. Reset link: {ResetLink}",
			user.Email,
			resetLink);

		return Task.CompletedTask;
	}

	public Task SendOrderConfirmationAsync(
		User user,
		Order order,
		string paymentMethod,
		string deliveryMethod)
	{
		string bankTransferNote = paymentMethod.Equals(
			"bank-transfer",
			StringComparison.OrdinalIgnoreCase)
			? $" IBAN: {_paymentOptions.BankTransfer.Iban}, Beneficiary: {_paymentOptions.BankTransfer.Beneficiary}."
			: string.Empty;

		logger.LogInformation(
			"Order confirmation prepared for {Email}. OrderId: {OrderId}, Status: {Status}, Payment: {PaymentMethod}, Delivery: {DeliveryMethod}.{BankTransferNote}",
			user.Email,
			order.Id,
			order.Status,
			paymentMethod,
			deliveryMethod,
			bankTransferNote);

		return Task.CompletedTask;
	}

	public Task SendOrderStatusChangedAsync(User user, Order order)
	{
		logger.LogInformation(
			"Order status update prepared for {Email}. OrderId: {OrderId}, NewStatus: {Status}",
			user.Email,
			order.Id,
			order.Status);

		return Task.CompletedTask;
	}

	public Task SendContactMessageAsync(
		string name,
		string email,
		string? phone,
		string subject,
		string message)
	{
		logger.LogInformation(
			"Contact message received. Name: {Name}, Email: {Email}, Phone: {Phone}, Subject: {Subject}, Message: {Message}",
			name,
			email,
			phone,
			subject,
			message);

		return Task.CompletedTask;
	}
}
