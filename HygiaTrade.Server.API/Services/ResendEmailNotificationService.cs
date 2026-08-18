using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using HygiaTrade.Common.Options;
using HygiaTrade.Data.Entities;
using HygiaTrade.Domain.Interfaces;

namespace HygiaTrade.API.Services;

public sealed class ResendEmailNotificationService(
	HttpClient httpClient,
	ILogger<ResendEmailNotificationService> logger,
	IOptions<EmailOptions> emailOptions,
	IOptions<PaymentOptions> paymentOptions) : IEmailNotificationService
{
	private const string ResendEndpoint = "https://api.resend.com/emails";

	private readonly EmailOptions _emailOptions = emailOptions.Value;
	private readonly PaymentOptions _paymentOptions = paymentOptions.Value;

	public Task SendPasswordResetAsync(User user, string resetLink)
	{
		string safeName = Encode(user.Names);
		string safeResetLink = Encode(resetLink);

		string html = $$"""
            <!doctype html>
            <html lang="bg">
            <body style="margin:0;background:#f4f6f8;font-family:Arial,sans-serif;color:#263b4d;">
              <div style="max-width:640px;margin:0 auto;padding:32px 16px;">
                <div style="background:#ffffff;border:1px solid #d6dde3;padding:32px;">
                  <h1 style="margin:0 0 16px;font-size:26px;">
                    Нулиране на парола
                  </h1>

                  <p style="margin:0 0 16px;line-height:1.6;">
                    Здравейте, {{safeName}}.
                  </p>

                  <p style="margin:0 0 24px;line-height:1.6;">
                    Получихме заявка за промяна на паролата на вашия
                    HygiaTrade профил.
                  </p>

                  <a
                    href="{{safeResetLink}}"
                    style="display:inline-block;background:#18b99f;color:#ffffff;text-decoration:none;font-weight:700;padding:14px 22px;">
                    Изберете нова парола
                  </a>

                  <p style="margin:24px 0 0;font-size:13px;line-height:1.6;color:#6f7f8c;">
                    Ако не сте изпращали тази заявка, не предприемайте действие.
                  </p>
                </div>
              </div>
            </body>
            </html>
            """;

		return SendEmailAsync(
			user.Email,
			"HygiaTrade – нулиране на парола",
			html);
	}

	public Task SendOrderConfirmationAsync(
		User user,
		Order order,
		string paymentMethod,
		string deliveryMethod)
	{
		string itemsHtml = order.Items.Count == 0
			? """
              <p style="color:#6f7f8c;">
                Няма заредени детайли за продуктите.
              </p>
              """
			: string.Join(
				string.Empty,
				order.Items.Select(item => $$"""
                    <tr>
                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;">
                        {{Encode(item.Title)}}
                      </td>

                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;text-align:center;">
                        {{item.Quantity}}
                      </td>

                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;text-align:right;">
                        {{FormatMoney(item.TotalPrice)}}
                      </td>
                    </tr>
                    """));

		string bankTransferHtml = paymentMethod.Equals(
			"bank-transfer",
			StringComparison.OrdinalIgnoreCase)
			? $$"""
                <div style="margin-top:24px;padding:18px;background:#f4f6f8;border:1px solid #d6dde3;">
                  <strong>Данни за банков превод</strong>

                  <p style="margin:10px 0 0;line-height:1.6;">
                    Получател:
                    {{Encode(_paymentOptions.BankTransfer.Beneficiary)}}
                    <br>

                    IBAN:
                    {{Encode(_paymentOptions.BankTransfer.Iban)}}
                    <br>

                    BIC:
                    {{Encode(_paymentOptions.BankTransfer.Bic)}}
                    <br>

                    Банка:
                    {{Encode(_paymentOptions.BankTransfer.BankName)}}
                  </p>
                </div>
                """
			: string.Empty;

		string html = $$"""
            <!doctype html>
            <html lang="bg">
            <body style="margin:0;background:#f4f6f8;font-family:Arial,sans-serif;color:#263b4d;">
              <div style="max-width:720px;margin:0 auto;padding:32px 16px;">
                <div style="background:#ffffff;border:1px solid #d6dde3;padding:32px;">
                  <h1 style="margin:0 0 16px;font-size:26px;">
                    Поръчката е приета
                  </h1>

                  <p style="margin:0 0 8px;line-height:1.6;">
                    Здравейте, {{Encode(user.Names)}}.
                  </p>

                  <p style="margin:0 0 24px;line-height:1.6;">
                    Поръчка <strong>#{{order.Id}}</strong>
                    е регистрирана успешно.
                  </p>

                  <table style="width:100%;border-collapse:collapse;font-size:14px;">
                    <thead>
                      <tr style="background:#f4f6f8;">
                        <th style="padding:10px;text-align:left;">
                          Продукт
                        </th>

                        <th style="padding:10px;text-align:center;">
                          Количество
                        </th>

                        <th style="padding:10px;text-align:right;">
                          Стойност
                        </th>
                      </tr>
                    </thead>

                    <tbody>
                      {{itemsHtml}}
                    </tbody>
                  </table>

                  <p style="margin:24px 0 0;line-height:1.7;">
                    <strong>Общо:</strong>
                    {{FormatMoney(order.OrderTotalPrice)}}
                    <br>

                    <strong>Плащане:</strong>
                    {{Encode(paymentMethod)}}
                    <br>

                    <strong>Доставка:</strong>
                    {{Encode(deliveryMethod)}}
                    <br>

                    <strong>Статус:</strong>
                    {{Encode(order.Status.ToString())}}
                  </p>

                  {{bankTransferHtml}}
                </div>
              </div>
            </body>
            </html>
            """;

		return SendEmailAsync(
			user.Email,
			$"HygiaTrade – потвърждение на поръчка #{order.Id}",
			html);
	}

	public Task SendOrderStatusChangedAsync(User user, Order order)
	{
		string html = $$"""
            <!doctype html>
            <html lang="bg">
            <body style="margin:0;background:#f4f6f8;font-family:Arial,sans-serif;color:#263b4d;">
              <div style="max-width:640px;margin:0 auto;padding:32px 16px;">
                <div style="background:#ffffff;border:1px solid #d6dde3;padding:32px;">
                  <h1 style="margin:0 0 16px;font-size:26px;">
                    Промяна по поръчката
                  </h1>

                  <p style="margin:0 0 16px;line-height:1.6;">
                    Здравейте, {{Encode(user.Names)}}.
                  </p>

                  <p style="margin:0;line-height:1.7;">
                    Статусът на поръчка
                    <strong>#{{order.Id}}</strong>
                    е променен на
                    <strong>{{Encode(order.Status.ToString())}}</strong>.
                  </p>
                </div>
              </div>
            </body>
            </html>
            """;

		return SendEmailAsync(
			user.Email,
			$"HygiaTrade – статус на поръчка #{order.Id}",
			html);
	}

	public Task SendContactMessageAsync(
		string name,
		string email,
		string? phone,
		string subject,
		string message)
	{
		string recipient =
			string.IsNullOrWhiteSpace(_emailOptions.ContactRecipientEmail)
				? _emailOptions.SenderEmail
				: _emailOptions.ContactRecipientEmail;

		string normalizedSubject = NormalizeSubject(subject);

		string html = $$"""
            <!doctype html>
            <html lang="bg">
            <body style="margin:0;background:#f4f6f8;font-family:Arial,sans-serif;color:#263b4d;">
              <div style="max-width:680px;margin:0 auto;padding:32px 16px;">
                <div style="background:#ffffff;border:1px solid #d6dde3;padding:32px;">
                  <h1 style="margin:0 0 22px;font-size:26px;">
                    Ново запитване от контактната форма
                  </h1>

                  <table style="width:100%;border-collapse:collapse;font-size:14px;">
                    <tr>
                      <td style="width:130px;padding:10px;border-bottom:1px solid #e5e7eb;font-weight:700;">
                        Име
                      </td>

                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;">
                        {{Encode(name)}}
                      </td>
                    </tr>

                    <tr>
                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;font-weight:700;">
                        Имейл
                      </td>

                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;">
                        {{Encode(email)}}
                      </td>
                    </tr>

                    <tr>
                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;font-weight:700;">
                        Телефон
                      </td>

                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;">
                        {{Encode(phone ?? string.Empty)}}
                      </td>
                    </tr>

                    <tr>
                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;font-weight:700;">
                        Тема
                      </td>

                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;">
                        {{Encode(normalizedSubject)}}
                      </td>
                    </tr>
                  </table>

                  <div style="margin-top:24px;padding:18px;background:#f4f6f8;border:1px solid #d6dde3;">
                    <strong>Съобщение</strong>

                    <p style="margin:12px 0 0;line-height:1.7;white-space:pre-wrap;">
                      {{Encode(message)}}
                    </p>
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;

		return SendEmailAsync(
			recipient,
			$"HygiaTrade contact – {normalizedSubject}",
			html,
			email);
	}

	private async Task SendEmailAsync(
		string recipient,
		string subject,
		string html,
		string? replyTo = null)
	{
		if (string.IsNullOrWhiteSpace(_emailOptions.ResendApiKey))
		{
			throw new InvalidOperationException(
				"The Resend API key is not configured.");
		}

		if (string.IsNullOrWhiteSpace(_emailOptions.SenderEmail))
		{
			throw new InvalidOperationException(
				"The sender email is not configured.");
		}

		if (string.IsNullOrWhiteSpace(recipient))
		{
			throw new InvalidOperationException(
				"The recipient email is not configured.");
		}

		Dictionary<string, object?> payload = new()
		{
			["from"] =
				$"{_emailOptions.SenderName} <{_emailOptions.SenderEmail}>",

			["to"] = new[]
			{
				recipient
			},

			["subject"] = NormalizeSubject(subject),
			["html"] = html
		};

		if (!string.IsNullOrWhiteSpace(replyTo))
		{
			payload["reply_to"] = replyTo.Trim();
		}

		using HttpRequestMessage request = new(
			HttpMethod.Post,
			ResendEndpoint)
		{
			Content = JsonContent.Create(payload)
		};

		request.Headers.Authorization = new AuthenticationHeaderValue(
			"Bearer",
			_emailOptions.ResendApiKey);

		using HttpResponseMessage response =
			await httpClient.SendAsync(request);

		if (response.IsSuccessStatusCode)
		{
			logger.LogInformation(
				"Email sent through Resend to {Recipient}. Subject: {Subject}",
				recipient,
				subject);

			return;
		}

		string responseBody =
			await response.Content.ReadAsStringAsync();

		logger.LogError(
			"Resend rejected an email to {Recipient}. Status: {StatusCode}. Response: {ResponseBody}",
			recipient,
			(int)response.StatusCode,
			responseBody);

		throw new HttpRequestException(
			$"Resend returned status {(int)response.StatusCode}: {responseBody}");
	}

	private static string Encode(string value)
	{
		return WebUtility.HtmlEncode(value);
	}

	private static string NormalizeSubject(string value)
	{
		string normalized = value
			.Replace('\r', ' ')
			.Replace('\n', ' ')
			.Trim();

		return string.IsNullOrWhiteSpace(normalized)
			? "Contact enquiry"
			: normalized;
	}

	private static string FormatMoney(decimal value)
	{
		return value.ToString(
			"C",
			CultureInfo.GetCultureInfo("bg-BG"));
	}
}
