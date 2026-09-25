using HygiaTrade.API.Services;
using HygiaTrade.Data.Entities;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class ResendEmailNotificationServiceTests
{
    private readonly Mock<IEmailNotificationTemplateBuilder> templates = new();
    private readonly Mock<IResendEmailTransport> transport = new();

    private ResendEmailNotificationService CreateService() =>
        new(templates.Object, transport.Object);

    [Fact]
    public async Task SendPasswordResetAsync_BuildsAndSends()
    {
        User user = User();
        var message = Message();

        templates
            .Setup(x => x.BuildPasswordReset(user, "reset"))
            .Returns(message);

        await CreateService().SendPasswordResetAsync(user, "reset");

        transport.Verify(
            x => x.SendAsync(
                message,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendOrderConfirmationAsync_BuildsAndSends()
    {
        User user = User();
        var order = new Order();
        var message = Message();

        templates
            .Setup(x => x.BuildOrderConfirmation(
                user,
                order,
                "bank-transfer",
                "courier"))
            .Returns(message);

        await CreateService().SendOrderConfirmationAsync(
            user,
            order,
            "bank-transfer",
            "courier");

        transport.Verify(
            x => x.SendAsync(
                message,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendOrderStatusChangedAsync_BuildsAndSends()
    {
        User user = User();
        var order = new Order();
        var message = Message();

        templates
            .Setup(x => x.BuildOrderStatusChanged(user, order))
            .Returns(message);

        await CreateService().SendOrderStatusChangedAsync(user, order);

        transport.Verify(
            x => x.SendAsync(
                message,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendContactMessageAsync_BuildsAndSends()
    {
        var outbound = Message();

        templates
            .Setup(x => x.BuildContactMessage(
                "Name",
                "sender@example.com",
                "123",
                "Subject",
                "Body"))
            .Returns(outbound);

        await CreateService().SendContactMessageAsync(
            "Name",
            "sender@example.com",
            "123",
            "Subject",
            "Body");

        transport.Verify(
            x => x.SendAsync(
                outbound,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static ResendEmailMessage Message() =>
        new("to@example.com", "Subject", "<p>Body</p>");

    private static User User() =>
        new()
        {
            Email = "user@example.com",
            Names = "User",
            Phone = "1"
        };
}
