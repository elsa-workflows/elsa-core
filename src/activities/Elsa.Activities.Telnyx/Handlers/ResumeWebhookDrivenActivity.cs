using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Activities;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Providers.Bookmarks;
using Elsa.Activities.Telnyx.Webhooks.Events;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Bookmarks;
using Elsa.Services;
using MediatR;

namespace Elsa.Activities.Telnyx.Handlers
{
    public abstract class ResumeWebhookDrivenActivity<TActivity, TPayload> : INotificationHandler<TelnyxWebhookReceived> where TPayload: CallPayload
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        protected ResumeWebhookDrivenActivity(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;
        protected virtual string ActivityTypeName => typeof(TActivity).Name;

        public async Task Handle(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
        {
            if (notification.Webhook.Data.Payload is not TPayload payload)
                return;
            
            var correlationId = GetCorrelationId(payload);
            var trigger = CreateBookmark();
            var bookmark = CreateBookmark();
            var context = new CollectWorkflowsContext(ActivityTypeName, bookmark, trigger, correlationId);
            await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(context, payload, cancellationToken);
        }
        
        protected virtual IBookmark CreateBookmark() => new GatherUsingSpeakBookmark();
        
        private string GetCorrelationId(TPayload payload)
        {
            if (!string.IsNullOrWhiteSpace(payload.ClientState))
            {
                var clientStatePayload = ClientStatePayload.FromBase64(payload.ClientState);
                return clientStatePayload.CorrelationId;
            }

            return payload.CallSessionId;
        }
    }
}