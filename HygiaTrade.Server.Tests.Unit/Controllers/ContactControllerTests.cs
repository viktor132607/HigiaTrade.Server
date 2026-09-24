using HygiaTrade.API.Controllers;
using HygiaTrade.Common.Requests.Contact;
using HygiaTrade.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Controllers;

public sealed class ContactControllerTests
{
    private readonly Mock<IContactService> contactService = new();

    private ContactController CreateController() =>
        new(contactService.Object);

    [Fact]
    public async Task SendAsync_ReturnsOk_WithSuccessMessage()
    {
        CreateContactRequest request = CreateRequest();

        contactService
            .Setup(service => service.SendAsync(request))
            .Returns(Task.CompletedTask);

        IActionResult result =
            await CreateController().SendAsync(request);

        OkObjectResult ok =
            Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(ok.Value);

        string? message = ok.Value
            .GetType()
            .GetProperty("message")
            ?.GetValue(ok.Value)
            ?.ToString();

        Assert.Equal(
            "Contact message sent successfully.",
            message);

        contactService.Verify(
            service => service.SendAsync(request),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_PropagatesException_FromService()
    {
        CreateContactRequest request = CreateRequest();
        InvalidOperationException expected =
            new("Email provider failed.");

        contactService
            .Setup(service => service.SendAsync(request))
            .ThrowsAsync(expected);

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateController().SendAsync(request));

        Assert.Same(expected, actual);

        contactService.Verify(
            service => service.SendAsync(request),
            Times.Once);
    }

    private static CreateContactRequest CreateRequest() => new()
    {
        Name = "Test User",
        Email = "user@example.com",
        Phone = "+359888123456",
        Subject = "Question",
        Message = "This is a valid contact message."
    };
}
