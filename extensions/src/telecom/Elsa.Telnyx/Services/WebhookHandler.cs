using System.Text;
using System.Text.Json;
using Elsa.Telnyx.Contracts;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Models;
using Elsa.Telnyx.Serialization;
using Elsa.Mediator;
using Elsa.Mediator.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Elsa.Telnyx.Services;

internal class WebhookHandler(INotificationSender notificationSender, ILogger<WebhookHandler> logger) : IWebhookHandler
{
    private static readonly JsonSerializerOptions SerializerSettings;

    static WebhookHandler()
    {
        SerializerSettings = CreateSerializerSettings();
    }

    public async Task HandleAsync(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        var json = await ReadRequestBodyAsync(httpContext);
        var webhook = JsonSerializer.Deserialize<TelnyxWebhook>(json, SerializerSettings)!;
        
        logger.LogDebug("Telnyx webhook payload received: {@Webhook}", webhook);
        await notificationSender.SendAsync(new TelnyxWebhookReceived(webhook), NotificationStrategy.Background, cancellationToken);
    }

    private static async Task<string> ReadRequestBodyAsync(HttpContext httpContext)
    {
        string body;
        var req = httpContext.Request;

        // Allows using several time the stream in ASP.Net Core
        req.EnableBuffering();

        // Arguments: Stream, Encoding, detect encoding, buffer size AND, the most important: keep stream opened.
        using (var reader = new StreamReader(req.Body, Encoding.UTF8, true, 1024, true)) 
            body = await reader.ReadToEndAsync();

        // Rewind, so the core is not lost when it looks at the body for the request
        req.Body.Position = 0;
        return body;
    }

    private static JsonSerializerOptions CreateSerializerSettings()
    {
        var settings = new JsonSerializerOptions
        {
            PropertyNamingPolicy = new SnakeCaseNamingPolicy()
        };
            
        settings.Converters.Add(new WebhookDataJsonConverter());
        return settings;
    }
}