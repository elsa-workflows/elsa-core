using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
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
            
            var correlationId = GetCorrelationId(payload);
            var toBookmark = new FilteredCallInitiatedToBookmark(payload.To);
            var toQuery = new WorkflowsQuery(nameof(FilteredCallInitiated), toBookmark, correlationId);
            var fromBookmark = new FilteredCallInitiatedToBookmark(payload.From);
            var fromQuery = new WorkflowsQuery(nameof(FilteredCallInitiated), fromBookmark, correlationId);

            await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(toQuery, new WorkflowInput(webhook), cancellationToken);
            await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(fromQuery, new WorkflowInput(webhook), cancellationToken);
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