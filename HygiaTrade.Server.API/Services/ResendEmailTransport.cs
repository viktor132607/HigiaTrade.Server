using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using HygiaTrade.Common.Options;

namespace HygiaTrade.API.Services;

public sealed record ResendEmailMessage(
	string Recipient,
	string Subject,
	string Html,
	string? ReplyTo = null);

public interface IResendEmailTransport
{
	Task SendAsync(
		ResendEmailMessage message,
		CancellationToken cancellationToken = default);
}

public sealed class ResendEmailTransport(
	HttpClient httpClient,
	ILogger<ResendEmailTransport> logger,
	IOptions<EmailOptions> emailOptions)
	: IResendEmailTransport
{
	private const string ResendEndpoint =
		"https://api.resend.com/emails";

	private readonly EmailOptions options =
		emailOptions.Value;

	public async Task SendAsync(
		ResendEmailMessage message,
		CancellationToken cancellationToken = default)
	{
		ValidateConfiguration(message);

		Dictionary<string, object?> payload =
			CreatePayload(message);

		using HttpRequestMessage request = new(
			HttpMethod.Post,
			ResendEndpoint)
		{
			Content = JsonContent.Create(payload)
		};

		request.Headers.Authorization =
			new AuthenticationHeaderValue(
				"Bearer",
				options.ResendApiKey);

		using HttpResponseMessage response =
			await httpClient.SendAsync(
				request,
				cancellationToken);

		if (response.IsSuccessStatusCode)
		{
			logger.LogInformation(
				"Email sent through Resend to {Recipient}. Subject: {Subject}",
				message.Recipient,
				message.Subject);

			return;
		}

		string responseBody =
			await response.Content.ReadAsStringAsync(
				cancellationToken);

		logger.LogError(
			"Resend rejected an email to {Recipient}. Status: {StatusCode}. Response: {ResponseBody}",
			message.Recipient,
			(int)response.StatusCode,
			responseBody);

		throw new HttpRequestException(
			$"Resend returned status {(int)response.StatusCode}: {responseBody}");
	}

	private void ValidateConfiguration(
		ResendEmailMessage message)
	{
		if (string.IsNullOrWhiteSpace(
				options.ResendApiKey))
		{
			throw new InvalidOperationException(
				"The Resend API key is not configured.");
		}

		if (string.IsNullOrWhiteSpace(
				options.SenderEmail))
		{
			throw new InvalidOperationException(
				"The sender email is not configured.");
		}

		if (string.IsNullOrWhiteSpace(
				message.Recipient))
		{
			throw new InvalidOperationException(
				"The recipient email is not configured.");
		}
	}

	private Dictionary<string, object?> CreatePayload(
		ResendEmailMessage message)
	{
		var payload =
			new Dictionary<string, object?>
			{
				["from"] =
					$"{options.SenderName} <{options.SenderEmail}>",

				["to"] = new[]
				{
					message.Recipient
				},

				["subject"] =
					EmailContentFormatter
						.NormalizeSubject(
							message.Subject),

				["html"] = message.Html
			};

		if (!string.IsNullOrWhiteSpace(
				message.ReplyTo))
		{
			payload["reply_to"] =
				message.ReplyTo.Trim();
		}

		return payload;
	}
}
