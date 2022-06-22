using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Extensions;
using Elsa.Activities.Telnyx.Providers.Bookmarks;
using Elsa.Activities.Telnyx.Webhooks.Events;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using MediatR;

namespace Elsa.Activities.Telnyx.Webhooks.Handlers
{
    internal class TriggerFilteredCallInitiatedActivities : INotificationHandler<TelnyxWebhookReceived>
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;

        public TriggerFilteredCallInitiatedActivities(IWorkflowLaunchpad workflowLaunchpad)
        {
            _workflowLaunchpad = workflowLaunchpad;
        }

        public async Task Handle(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
        {
            var webhook = notification.Webhook;
            
            if (webhook.Data.Payload is not CallInitiatedPayload payload)
                return;

            if (payload.Direction != "incoming")
                return;
            
            var correlationId = payload.GetCorrelationId();
            var toBookmark = new FilteredCallInitiatedToBookmark(payload.To);
            var toQuery = new WorkflowsQuery(nameof(FilteredCallInitiated), toBookmark, correlationId);
            var fromBookmark = new FilteredCallInitiatedFromBookmark(payload.From);
            var fromQuery = new WorkflowsQuery(nameof(FilteredCallInitiated), fromBookmark, correlationId);

            await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(toQuery, new WorkflowInput(webhook), cancellationToken);
            await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(fromQuery, new WorkflowInput(webhook), cancellationToken);
        }
    }
}