using System.Text;
using System.Text.Json;
using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Contracts;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Models;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Telnyx.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Elsa.Telnyx.Services;

internal class WebhookHandler : IWebhookHandler
{
    private static readonly JsonSerializerOptions SerializerSettings;
    private readonly IBackgroundEventPublisher _mediator;
    private readonly ILogger<WebhookHandler> _logger;

    static WebhookHandler()
    {
        SerializerSettings = CreateSerializerSettings();
    }

    public WebhookHandler(IBackgroundEventPublisher mediator, ILogger<WebhookHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task HandleAsync(HttpContext httpContext)
    {
        var cancellationToken = httpContext.RequestAborted;
        var json = await ReadRequestBodyAsync(httpContext);
        var webhook = JsonSerializer.Deserialize<TelnyxWebhook>(json, SerializerSettings)!;
        var correlationId = ((Payload)webhook.Data.Payload).GetCorrelationId();

        using var loggingScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });
        _logger.LogDebug("Telnyx webhook payload received: {@Webhook}", webhook);
        await _mediator.PublishAsync(new TelnyxWebhookReceived(webhook), cancellationToken);
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