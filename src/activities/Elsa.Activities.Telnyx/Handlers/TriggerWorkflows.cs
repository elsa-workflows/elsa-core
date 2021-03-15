using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Telnyx.Attributes;
using Elsa.Activities.Telnyx.Bookmarks;
using Elsa.Activities.Telnyx.Events;
using Elsa.Services;
using MediatR;

namespace Elsa.Activities.Telnyx.Handlers
{
    internal class TriggerWorkflows : INotificationHandler<TelnyxWebhookReceived>
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string TenantId = default;

        private readonly IWorkflowRunner _workflowRunner;

        public TriggerWorkflows(IWorkflowRunner workflowRunner)
        {
            _workflowRunner = workflowRunner;
        }

        public async Task Handle(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
        {
            var webhook = notification.Webhook;
            var eventType = webhook.Data.EventType;
            var payload = notification.Webhook.Data.Payload;
            var payloadAttribute = payload.GetType().GetCustomAttribute<PayloadAttribute>()!;
            var activityType = payloadAttribute.ActivityType;
            var correlationId = default(string);

            await _workflowRunner.StartWorkflowsAsync(activityType, new NotificationBookmark(eventType), TenantId, webhook, cancellationToken: cancellationToken);
            await _workflowRunner.ResumeWorkflowsAsync(activityType, new NotificationBookmark(eventType, correlationId), TenantId, payload, correlationId, cancellationToken: cancellationToken);
        }
    }
}