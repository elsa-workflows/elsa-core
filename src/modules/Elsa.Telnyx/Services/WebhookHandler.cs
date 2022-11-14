using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using Dahomey.Json.NamingPolicies;
using Elsa.Mediator.Services;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Elsa.Telnyx.Services
{
    internal class WebhookHandler : IWebhookHandler
    {
        private static readonly JsonSerializerOptions SerializerSettings;
        private readonly IMediator _mediator;
        private readonly ILogger<WebhookHandler> _logger;

        static WebhookHandler()
        {
            SerializerSettings = CreateSerializerSettings();
        }

        public WebhookHandler(IMediator mediator, ILogger<WebhookHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public async Task HandleAsync(HttpContext httpContext)
        {
            var cancellationToken = httpContext.RequestAborted;
            var json = await ReadRequestBodyAsync(httpContext);
            var webhook = JsonSerializer.Deserialize<TelnyxWebhook>(json, SerializerSettings)!;
            var correlationId = webhook.Data.Payload.GetCorrelationId();
            
            if (!string.IsNullOrEmpty(correlationId))
            {
                using var loggingScope = _logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });
                _logger.LogDebug("Telnyx webhook payload received: {@Webhook}", webhook);
                await _mediator.PublishAsync(new TelnyxWebhookReceived(webhook), cancellationToken);
            }
            else
            {
                _logger.LogDebug("Telnyx webhook payload received: {@Webhook}", webhook);
                await _mediator.PublishAsync(new TelnyxWebhookReceived(webhook), cancellationToken);
            }
        }

        private static async Task<string> ReadRequestBodyAsync(HttpContext httpContext)
        {
            var cancellationToken = httpContext.RequestAborted;
            var readResult = default(ReadResult);

            try
            {
                readResult = await httpContext.Request.BodyReader.ReadAsync(cancellationToken);
                return Encoding.UTF8.GetString(readResult.Buffer);
            }
            finally
            {
                httpContext.Request.BodyReader.AdvanceTo(readResult.Buffer.End);
            }
        }

        private static JsonSerializerOptions CreateSerializerSettings()
        {
            var settings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new SnakeCaseNamingPolicy()
            };
            return settings;
        }
    }
}