using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Providers.Bookmarks;
using Elsa.Activities.Telnyx.Webhooks.Events;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Abstract;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Activities.Telnyx.Webhooks.Services;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities.Telnyx.Webhooks.Handlers
{
    internal class TriggerWebhookActivities : INotificationHandler<TelnyxWebhookReceived>
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        private readonly IWebhookFilterService _webhookFilterService;
        private readonly ILogger _logger;

        public TriggerWebhookActivities(IWorkflowLaunchpad workflowLaunchpad, IWebhookFilterService webhookFilterService, ILogger<TriggerWebhookActivities> logger)
        {
            _workflowLaunchpad = workflowLaunchpad;
            _webhookFilterService = webhookFilterService;
            _logger = logger;
        }

        public async Task Handle(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
        {
            var webhook = notification.Webhook;
            var eventType = webhook.Data.EventType;
            var payload = webhook.Data.Payload;
            var activityType = _webhookFilterService.GetActivityTypeName(payload);

            if (activityType == null)
            {
                _logger.LogWarning("The received event '{EventType}' is an unsupported event", webhook.Data.EventType);
                return;
            }

            var correlationId = GetCorrelationId(payload);
            var bookmark = new NotificationBookmark(eventType);
            var context = new WorkflowsQuery(activityType, bookmark, correlationId);

            await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(context, new WorkflowInput(webhook), cancellationToken);
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