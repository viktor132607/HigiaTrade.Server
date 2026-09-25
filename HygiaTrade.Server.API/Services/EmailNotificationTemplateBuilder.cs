using Microsoft.Extensions.Options;
using HygiaTrade.Common.Options;
using HygiaTrade.Data.Entities;

namespace HygiaTrade.API.Services;

public interface IEmailNotificationTemplateBuilder
{
	ResendEmailMessage BuildPasswordReset(
		User user,
		string resetLink);

	ResendEmailMessage BuildOrderConfirmation(
		User user,
		Order order,
		string paymentMethod,
		string deliveryMethod);

	ResendEmailMessage BuildOrderStatusChanged(
		User user,
		Order order);

	ResendEmailMessage BuildContactMessage(
		string name,
		string email,
		string? phone,
		string subject,
		string message);
}

public sealed class EmailNotificationTemplateBuilder(
	IOptions<EmailOptions> emailOptions,
	IOptions<PaymentOptions> paymentOptions)
	: IEmailNotificationTemplateBuilder
{
	private readonly EmailOptions email =
		emailOptions.Value;

	private readonly PaymentOptions payment =
		paymentOptions.Value;

	public ResendEmailMessage BuildPasswordReset(
		User user,
		string resetLink)
	{
		string safeName =
			EmailContentFormatter.Encode(user.Names);

		string safeResetLink =
			EmailContentFormatter.Encode(resetLink);

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

		return new ResendEmailMessage(
			user.Email,
			"HygiaTrade – нулиране на парола",
			html);
	}

	public ResendEmailMessage BuildOrderConfirmation(
		User user,
		Order order,
		string paymentMethod,
		string deliveryMethod)
	{
		string itemsHtml =
			BuildOrderItemsHtml(order);

		string bankTransferHtml =
			BuildBankTransferHtml(paymentMethod);

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
                    Здравейте, {{EmailContentFormatter.Encode(user.Names)}}.
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
                    {{EmailContentFormatter.FormatMoney(order.OrderTotalPrice)}}
                    <br>

                    <strong>Плащане:</strong>
                    {{EmailContentFormatter.Encode(paymentMethod)}}
                    <br>

                    <strong>Доставка:</strong>
                    {{EmailContentFormatter.Encode(deliveryMethod)}}
                    <br>

                    <strong>Статус:</strong>
                    {{EmailContentFormatter.Encode(order.Status.ToString())}}
                  </p>

                  {{bankTransferHtml}}
                </div>
              </div>
            </body>
            </html>
            """;

		return new ResendEmailMessage(
			user.Email,
			$"HygiaTrade – потвърждение на поръчка #{order.Id}",
			html);
	}

	public ResendEmailMessage BuildOrderStatusChanged(
		User user,
		Order order)
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
                    Здравейте, {{EmailContentFormatter.Encode(user.Names)}}.
                  </p>

                  <p style="margin:0;line-height:1.7;">
                    Статусът на поръчка
                    <strong>#{{order.Id}}</strong>
                    е променен на
                    <strong>{{EmailContentFormatter.Encode(order.Status.ToString())}}</strong>.
                  </p>
                </div>
              </div>
            </body>
            </html>
            """;

		return new ResendEmailMessage(
			user.Email,
			$"HygiaTrade – статус на поръчка #{order.Id}",
			html);
	}

	public ResendEmailMessage BuildContactMessage(
		string name,
		string emailAddress,
		string? phone,
		string subject,
		string message)
	{
		string recipient =
			string.IsNullOrWhiteSpace(
				email.ContactRecipientEmail)
				? email.SenderEmail
				: email.ContactRecipientEmail;

		string normalizedSubject =
			EmailContentFormatter
				.NormalizeSubject(subject);

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
                        {{EmailContentFormatter.Encode(name)}}
                      </td>
                    </tr>

                    <tr>
                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;font-weight:700;">
                        Имейл
                      </td>

                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;">
                        {{EmailContentFormatter.Encode(emailAddress)}}
                      </td>
                    </tr>

                    <tr>
                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;font-weight:700;">
                        Телефон
                      </td>

                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;">
                        {{EmailContentFormatter.Encode(phone ?? string.Empty)}}
                      </td>
                    </tr>

                    <tr>
                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;font-weight:700;">
                        Тема
                      </td>

                      <td style="padding:10px;border-bottom:1px solid #e5e7eb;">
                        {{EmailContentFormatter.Encode(normalizedSubject)}}
                      </td>
                    </tr>
                  </table>

                  <div style="margin-top:24px;padding:18px;background:#f4f6f8;border:1px solid #d6dde3;">
                    <strong>Съобщение</strong>

                    <p style="margin:12px 0 0;line-height:1.7;white-space:pre-wrap;">
                      {{EmailContentFormatter.Encode(message)}}
                    </p>
                  </div>
                </div>
              </div>
            </body>
            </html>
            """;

		return new ResendEmailMessage(
			recipient,
			$"HygiaTrade contact – {normalizedSubject}",
			html,
			emailAddress);
	}

	private string BuildOrderItemsHtml(Order order)
	{
		if (order.Items.Count == 0)
		{
			return """
              <p style="color:#6f7f8c;">
                Няма заредени детайли за продуктите.
              </p>
              """;
		}

		return string.Join(
			string.Empty,
			order.Items.Select(item => $$"""
                <tr>
                  <td style="padding:10px;border-bottom:1px solid #e5e7eb;">
                    {{EmailContentFormatter.Encode(item.Title)}}
                  </td>

                  <td style="padding:10px;border-bottom:1px solid #e5e7eb;text-align:center;">
                    {{item.Quantity}}
                  </td>

                  <td style="padding:10px;border-bottom:1px solid #e5e7eb;text-align:right;">
                    {{EmailContentFormatter.FormatMoney(item.TotalPrice)}}
                  </td>
                </tr>
                """));
	}

	private string BuildBankTransferHtml(
		string paymentMethod)
	{
		bool configured =
			paymentMethod.Equals(
				"bank-transfer",
				StringComparison.OrdinalIgnoreCase)
			&& !string.IsNullOrWhiteSpace(
				payment.BankTransfer.Iban)
			&& !payment.BankTransfer.Iban.Contains(
				"DEMO",
				StringComparison.OrdinalIgnoreCase);

		if (!configured)
		{
			return string.Empty;
		}

		return $$"""
            <div style="margin-top:24px;padding:18px;background:#f4f6f8;border:1px solid #d6dde3;">
              <strong>Данни за банков превод</strong>

              <p style="margin:10px 0 0;line-height:1.6;">
                Получател:
                {{EmailContentFormatter.Encode(payment.BankTransfer.Beneficiary)}}
                <br>

                IBAN:
                {{EmailContentFormatter.Encode(payment.BankTransfer.Iban)}}
                <br>

                BIC:
                {{EmailContentFormatter.Encode(payment.BankTransfer.Bic)}}
                <br>

                Банка:
                {{EmailContentFormatter.Encode(payment.BankTransfer.BankName)}}
              </p>
            </div>
            """;
	}
}
