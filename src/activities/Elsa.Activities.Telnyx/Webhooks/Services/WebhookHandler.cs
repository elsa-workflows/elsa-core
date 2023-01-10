using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Activities.Telnyx.Webhooks.Events;
using Elsa.Activities.Telnyx.Webhooks.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.JsonNet;

namespace Elsa.Activities.Telnyx.Webhooks.Services
{
    internal class WebhookHandler : IWebhookHandler
    {
        private static readonly JsonSerializerSettings SerializerSettings;
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
            var webhook = JsonConvert.DeserializeObject<TelnyxWebhook>(json, SerializerSettings)!;
            var correlationId = webhook.Data.Payload.GetCorrelationId();
            
            if (!string.IsNullOrEmpty(correlationId))
            {
                using var loggingScope = _logger.BeginScope(new TelnyxWebhookLogScope(correlationId));
                _logger.LogDebug("Telnyx webhook payload received: {@Webhook}", webhook);
                await _mediator.Publish(new TelnyxWebhookReceived(webhook), cancellationToken);
            }
            else
            {
                _logger.LogDebug("Telnyx webhook payload received: {@Webhook}", webhook);
                await _mediator.Publish(new TelnyxWebhookReceived(webhook), cancellationToken);
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

        private static JsonSerializerSettings CreateSerializerSettings()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };

            settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            return settings;
        }
    }
}