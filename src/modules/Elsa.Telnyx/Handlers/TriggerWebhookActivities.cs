using Elsa.Mediator.Services;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Telnyx.Handlers
{
    internal class TriggerWebhookActivities : INotificationHandler<TelnyxWebhookReceived>
    {
        private readonly IWebhookFilterService _webhookFilterService;
        private readonly ILogger _logger;

        public TriggerWebhookActivities(IWebhookFilterService webhookFilterService, ILogger<TriggerWebhookActivities> logger)
        {
            _webhookFilterService = webhookFilterService;
            _logger = logger;
        }

        public async Task HandleAsync(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
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

            var correlationId = payload.GetCorrelationId();
            // var bookmark = new NotificationBookmark(eventType);
            // var context = new WorkflowsQuery(activityType, bookmark, correlationId);
            // await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(context, new WorkflowInput(webhook), cancellationToken).ToList();
        }
    }
}