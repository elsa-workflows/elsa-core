using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Activities.Telnyx.Providers.Bookmarks;
using Elsa.Activities.Telnyx.Webhooks.Events;
using Elsa.Activities.Telnyx.Webhooks.Services;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

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

            var correlationId = payload.GetCorrelationId();
            var bookmark = new NotificationBookmark(eventType);
            var context = new WorkflowsQuery(activityType, bookmark, correlationId);
            await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(context, new WorkflowInput(webhook), cancellationToken).ToList();
        }
    }
}