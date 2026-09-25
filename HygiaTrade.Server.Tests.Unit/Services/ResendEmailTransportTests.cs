using System.Net;
using System.Net.Http;
using System.Text.Json;
using HygiaTrade.API.Services;
using HygiaTrade.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace HygiaTrade.Server.Tests.Unit.Services;

public sealed class ResendEmailTransportTests
{
    [Fact]
    public async Task SendAsync_RejectsMissingApiKey()
    {
        var transport = CreateTransport(
            new EmailOptions
            {
                ResendApiKey = " ",
                SenderEmail = "sender@example.com"
            });

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => transport.SendAsync(Message()));

        Assert.Equal(
            "The Resend API key is not configured.",
            ex.Message);
    }

    [Fact]
    public async Task SendAsync_RejectsMissingSender()
    {
        var transport = CreateTransport(
            new EmailOptions
            {
                ResendApiKey = "key",
                SenderEmail = " "
            });

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => transport.SendAsync(Message()));

        Assert.Equal(
            "The sender email is not configured.",
            ex.Message);
    }

    [Fact]
    public async Task SendAsync_RejectsMissingRecipient()
    {
        var transport = CreateTransport(
            ValidOptions());

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => transport.SendAsync(
                    new ResendEmailMessage(
                        " ",
                        "Subject",
                        "<p>Body</p>")));

        Assert.Equal(
            "The recipient email is not configured.",
            ex.Message);
    }

    [Fact]
    public async Task SendAsync_SendsExpectedResendRequest()
    {
        HttpRequestSnapshot? captured = null;

        var handler = new DelegateHandler(
            async request =>
            {
                captured = await HttpRequestSnapshot.CreateAsync(request);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var transport =
            CreateTransport(
                ValidOptions(),
                handler);

        await transport.SendAsync(
            new ResendEmailMessage(
                "to@example.com",
                " Subject\r\n ",
                "<p>Body</p>",
                " reply@example.com "));

        Assert.NotNull(captured);
        Assert.Equal(
            "https://api.resend.com/emails",
            captured!.Uri);
        Assert.Equal(HttpMethod.Post, captured.Method);
        Assert.Equal("Bearer", captured.AuthScheme);
        Assert.Equal("key", captured.AuthParameter);

        using JsonDocument json =
            JsonDocument.Parse(captured.Body);

        JsonElement root = json.RootElement;

        Assert.Equal(
            "HygiaTrade <sender@example.com>",
            root.GetProperty("from").GetString());

        Assert.Equal(
            "to@example.com",
            root.GetProperty("to")[0].GetString());

        Assert.Equal(
            "Subject",
            root.GetProperty("subject").GetString());

        Assert.Equal(
            "<p>Body</p>",
            root.GetProperty("html").GetString());

        Assert.Equal(
            "reply@example.com",
            root.GetProperty("reply_to").GetString());
    }

    [Fact]
    public async Task SendAsync_OmitsReplyTo_WhenBlank()
    {
        string? body = null;

        var handler = new DelegateHandler(
            async request =>
            {
                body = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            });

        var transport =
            CreateTransport(
                ValidOptions(),
                handler);

        await transport.SendAsync(
            new ResendEmailMessage(
                "to@example.com",
                "Subject",
                "<p>Body</p>",
                " "));

        using JsonDocument json =
            JsonDocument.Parse(body!);

        Assert.False(
            json.RootElement.TryGetProperty(
                "reply_to",
                out _));
    }

    [Fact]
    public async Task SendAsync_ThrowsHttpRequestException_WhenResendRejects()
    {
        var handler = new DelegateHandler(
            _ => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content =
                        new StringContent("invalid payload")
                }));

        var transport =
            CreateTransport(
                ValidOptions(),
                handler);

        HttpRequestException ex =
            await Assert.ThrowsAsync<HttpRequestException>(
                () => transport.SendAsync(Message()));

        Assert.Contains("400", ex.Message);
        Assert.Contains("invalid payload", ex.Message);
    }

    private static ResendEmailTransport CreateTransport(
        EmailOptions options,
        HttpMessageHandler? handler = null)
    {
        HttpClient client =
            new(handler ??
                new DelegateHandler(
                    _ => Task.FromResult(
                        new HttpResponseMessage(
                            HttpStatusCode.OK))));

        return new ResendEmailTransport(
            client,
            Mock.Of<ILogger<ResendEmailTransport>>(),
            Options.Create(options));
    }

    private static EmailOptions ValidOptions() =>
        new()
        {
            ResendApiKey = "key",
            SenderEmail = "sender@example.com",
            SenderName = "HygiaTrade"
        };

    private static ResendEmailMessage Message() =>
        new(
            "to@example.com",
            "Subject",
            "<p>Body</p>");

    private sealed class DelegateHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            handler(request);
    }

    private sealed record HttpRequestSnapshot(
        string Uri,
        HttpMethod Method,
        string? AuthScheme,
        string? AuthParameter,
        string Body)
    {
        public static async Task<HttpRequestSnapshot> CreateAsync(
            HttpRequestMessage request) =>
            new(
                request.RequestUri!.ToString(),
                request.Method,
                request.Headers.Authorization?.Scheme,
                request.Headers.Authorization?.Parameter,
                await request.Content!.ReadAsStringAsync());
    }
}
