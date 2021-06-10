using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Models;
using Elsa.Activities.Telnyx.Providers.Bookmarks;
using Elsa.Activities.Telnyx.Webhooks.Events;
using Elsa.Activities.Telnyx.Webhooks.Payloads.Call;
using Elsa.Services;
using Elsa.Services.Bookmarks;
using MediatR;

namespace Elsa.Activities.Telnyx.Handlers
{
    public abstract class ResumeWebhookDrivenActivity<TActivity, TPayload> : ResumeWebhookDrivenActivity<TActivity> where TPayload : CallPayload
    {
        protected ResumeWebhookDrivenActivity(IWorkflowLaunchpad workflowLaunchpad) : base(workflowLaunchpad)
        {
        }

        protected override IEnumerable<Type> GetSupportedPayloadTypes() => new[] {typeof(TPayload)};
    }

    public abstract class ResumeWebhookDrivenActivity<TActivity> : INotificationHandler<TelnyxWebhookReceived>
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        protected ResumeWebhookDrivenActivity(IWorkflowLaunchpad workflowLaunchpad) => _workflowLaunchpad = workflowLaunchpad;
        protected virtual string ActivityTypeName => typeof(TActivity).Name;
        protected abstract IEnumerable<Type> GetSupportedPayloadTypes();

        public async Task Handle(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
        {
            var supportedPayloadTypes = GetSupportedPayloadTypes().ToHashSet();
            var receivedPayload = (CallPayload) notification.Webhook.Data.Payload;
            var receivedPayloadType = receivedPayload.GetType();

            if (!supportedPayloadTypes.Contains(receivedPayloadType))
                return;

            var correlationId = GetCorrelationId(receivedPayload);
            var trigger = CreateBookmark();
            var bookmark = CreateBookmark();
            var context = new CollectWorkflowsContext(ActivityTypeName, bookmark, trigger, correlationId);
            await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(context, receivedPayload, cancellationToken);
        }

        protected virtual IBookmark CreateBookmark() => new GatherUsingSpeakBookmark();

        private string GetCorrelationId(CallPayload payload)
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