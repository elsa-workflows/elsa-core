using System;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Bookmarks;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Webhooks.Events;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Activities.Telnyx.Webhooks.Services;
using Elsa.Dispatch;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Elsa.Activities.Telnyx.Webhooks.Consumers
{
    internal class TriggerWorkflows : IHandleMessages<TelnyxWebhookReceived>
    {
        // TODO: Design multi-tenancy. 
        private const string TenantId = default;

        private readonly IWorkflowDispatcher _workflowDispatcher;
        private readonly IWebhookFilterService _webhookFilterService;
        private readonly ILogger<TriggerWorkflows> _logger;

        public TriggerWorkflows(
            IWorkflowDispatcher workflowDispatcher,
            IWebhookFilterService webhookFilterService,
            ILogger<TriggerWorkflows> logger)
        {
            _workflowDispatcher = workflowDispatcher;
            _webhookFilterService = webhookFilterService;
            _logger = logger;
        }

        public async Task Handle(TelnyxWebhookReceived message)
        {
            var webhook = message.Webhook;
            var eventType = webhook.Data.EventType;
            var payload = message.Webhook.Data.Payload;
            var activityType = _webhookFilterService.GetActivityTypeName(payload);

            if (activityType == null)
            {
                _logger.LogWarning("The received event '{EventType}' is an unsupported event", webhook.Data.EventType);
                return;
            }

            var correlationId = GetCorrelationId(payload);

            var bookmark = new NotificationBookmark(eventType, correlationId);
            var trigger = new NotificationBookmark(eventType);

            await _workflowDispatcher.DispatchAsync(new TriggerWorkflowsRequest(activityType, bookmark, trigger, webhook, correlationId, TenantId: TenantId));
        }

        private string GetCorrelationId(Payload payload)
        {
            if (!string.IsNullOrWhiteSpace(payload.ClientState))
            {
                var clientStatePayload = ClientStatePayload.FromBase64(payload.ClientState);
                return clientStatePayload.CorrelationId;
            }

            if (payload is CallPayload callPayload)
                return callPayload.CallSessionId;

            throw new NotSupportedException($"The received payload type {payload.GetType().Name} is not supported yet.");
        }
    }
}