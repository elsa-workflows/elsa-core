using System;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Providers.Bookmarks;
using Elsa.Activities.Telnyx.Webhooks.Events;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Activities.Telnyx.Webhooks.Services;
using Elsa.Services;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;

namespace Elsa.Activities.Telnyx.Webhooks.Consumers
{
    internal class TriggerWebhookActivities : IHandleMessages<TelnyxWebhookReceived>
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        private readonly IWebhookFilterService _webhookFilterService;
        private readonly ILogger _logger;

        public TriggerWebhookActivities(
            IWorkflowLaunchpad workflowDispatcher,
            IWebhookFilterService webhookFilterService,
            ILogger<TriggerWebhookActivities> logger)
        {
            _workflowLaunchpad = workflowDispatcher;
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
            var context = new CollectWorkflowsContext(activityType, bookmark, trigger, correlationId);

            await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(context, webhook);
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