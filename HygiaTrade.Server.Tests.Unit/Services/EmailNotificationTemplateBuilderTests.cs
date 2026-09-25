using HygiaTrade.API.Services;
using HygiaTrade.Common.Options;
using HygiaTrade.Core.Enums;
using HygiaTrade.Data.Entities;
using Microsoft.Extensions.Options;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class EmailNotificationTemplateBuilderTests
{
    [Fact]
    public void BuildPasswordReset_EncodesNameAndLink()
    {
        var builder = CreateBuilder();
        User user = User();
        user.Names = "<Admin>";

        ResendEmailMessage result =
            builder.BuildPasswordReset(
                user,
                "https://example.com/?a=1&b=2");

        Assert.Equal(user.Email, result.Recipient);
        Assert.Contains("&lt;Admin&gt;", result.Html);
        Assert.Contains("&amp;b=2", result.Html);
        Assert.Equal(
            "HygiaTrade – нулиране на парола",
            result.Subject);
        Assert.Null(result.ReplyTo);
    }

    [Fact]
    public void BuildOrderConfirmation_ShowsEmptyItemsMessage()
    {
        var builder = CreateBuilder();
        User user = User();
        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderTotalPrice = 15m,
            Status = OrderStatus.Created
        };

        ResendEmailMessage result =
            builder.BuildOrderConfirmation(
                user,
                order,
                "online-card",
                "courier");

        Assert.Contains(
            "Няма заредени детайли за продуктите.",
            result.Html);

        Assert.DoesNotContain(
            "Данни за банков превод",
            result.Html);
    }

    [Fact]
    public void BuildOrderConfirmation_EncodesItemsAndShowsConfiguredBankTransfer()
    {
        var payment = new PaymentOptions
        {
            BankTransfer = new BankTransferOptions
            {
                Beneficiary = "<Hygia>",
                Iban = "BG80BANK1234567890",
                Bic = "BANKBGSF",
                BankName = "Bank & Co"
            }
        };

        var builder = CreateBuilder(payment: payment);

        User user = User();
        user.Names = "A&B";

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderTotalPrice = 24m,
            Status = OrderStatus.PendingVerification,
            Items =
            [
                new OrderItem
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2,
                    SinglePrice = 12m,
                    TotalPrice = 24m,
                    Title = "<Product>",
                    PrimaryImageUri = "img"
                }
            ]
        };

        ResendEmailMessage result =
            builder.BuildOrderConfirmation(
                user,
                order,
                "bank-transfer",
                "standard-courier");

        Assert.Contains("A&amp;B", result.Html);
        Assert.Contains("&lt;Product&gt;", result.Html);
        Assert.Contains("Данни за банков превод", result.Html);
        Assert.Contains("&lt;Hygia&gt;", result.Html);
        Assert.Contains("Bank &amp; Co", result.Html);
        Assert.Contains("BG80BANK1234567890", result.Html);
    }

    [Fact]
    public void BuildOrderConfirmation_HidesDemoBankTransferConfiguration()
    {
        var builder = CreateBuilder();

        ResendEmailMessage result =
            builder.BuildOrderConfirmation(
                User(),
                new Order(),
                "bank-transfer",
                "courier");

        Assert.DoesNotContain(
            "Данни за банков превод",
            result.Html);
    }

    [Fact]
    public void BuildOrderStatusChanged_ContainsEncodedNameOrderAndStatus()
    {
        var builder = CreateBuilder();
        User user = User();
        user.Names = "<User>";

        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Delivered
        };

        ResendEmailMessage result =
            builder.BuildOrderStatusChanged(user, order);

        Assert.Equal(user.Email, result.Recipient);
        Assert.Contains("&lt;User&gt;", result.Html);
        Assert.Contains(order.Id.ToString(), result.Html);
        Assert.Contains("Delivered", result.Html);
        Assert.Contains(order.Id.ToString(), result.Subject);
    }

    [Fact]
    public void BuildContactMessage_UsesConfiguredRecipientAndReplyToAndNormalizesSubject()
    {
        var email = new EmailOptions
        {
            SenderEmail = "sender@example.com",
            ContactRecipientEmail = "contact@example.com"
        };

        var builder = CreateBuilder(email);

        ResendEmailMessage result =
            builder.BuildContactMessage(
                "<Name>",
                "reply@example.com",
                "<123>",
                " Hello\r\nWorld ",
                "<Message>");

        Assert.Equal("contact@example.com", result.Recipient);
        Assert.Equal(
            "HygiaTrade contact – Hello  World",
            result.Subject);
        Assert.Equal("reply@example.com", result.ReplyTo);
        Assert.Contains("&lt;Name&gt;", result.Html);
        Assert.Contains("&lt;123&gt;", result.Html);
        Assert.Contains("&lt;Message&gt;", result.Html);
    }

    [Fact]
    public void BuildContactMessage_FallsBackToSenderAndFallbackSubject()
    {
        var email = new EmailOptions
        {
            SenderEmail = "sender@example.com",
            ContactRecipientEmail = " "
        };

        var builder = CreateBuilder(email);

        ResendEmailMessage result =
            builder.BuildContactMessage(
                "Name",
                "reply@example.com",
                null,
                "\r\n",
                "Body");

        Assert.Equal("sender@example.com", result.Recipient);
        Assert.Equal(
            "HygiaTrade contact – Contact enquiry",
            result.Subject);
        Assert.Contains("Contact enquiry", result.Html);
    }

    private static EmailNotificationTemplateBuilder CreateBuilder(
        EmailOptions? email = null,
        PaymentOptions? payment = null) =>
        new(
            Options.Create(
                email ??
                new EmailOptions
                {
                    SenderEmail = "sender@example.com"
                }),
            Options.Create(payment ?? new PaymentOptions()));

    private static User User() =>
        new()
        {
            Email = "user@example.com",
            Names = "User",
            Phone = "1"
        };
}
