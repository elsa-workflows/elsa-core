using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Abstractions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using JetBrains.Annotations;

namespace Elsa.Telnyx.Handlers;

/// <summary>
/// Resumes all workflows blocked on activities that are waiting for a given webhook.
/// </summary>
[PublicAPI]
internal class TriggerWebhookDrivenActivities : INotificationHandler<TelnyxWebhookReceived>
{
    private readonly IWorkflowInbox _workflowInbox;
    private readonly IActivityRegistry _activityRegistry;

    public TriggerWebhookDrivenActivities(IWorkflowInbox workflowInbox, IActivityRegistry activityRegistry)
    {
        _workflowInbox = workflowInbox;
        _activityRegistry = activityRegistry;
    }

    public async Task HandleAsync(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
    {
        var webhook = notification.Webhook;
        var eventType = webhook.Data.EventType;
        var eventPayload = webhook.Data.Payload;
        var callPayload = eventPayload as CallPayload;
        var callControlId = callPayload?.CallControlId;
        var input = new Dictionary<string, object>().AddInput(eventPayload.GetType().Name, eventPayload);
        var activityDescriptors = FindActivityDescriptors(eventType).ToList();
        var clientStatePayload = ((Payload)webhook.Data.Payload).GetClientStatePayload();
        var activityInstanceId = clientStatePayload?.ActivityInstanceId;
        var workflowInstanceId = clientStatePayload?.WorkflowInstanceId;
        var bookmarkPayloadWithCallControl = new WebhookEventBookmarkPayload(eventType, callControlId);

        foreach (var activityDescriptor in activityDescriptors)
        {
            await _workflowInbox.SubmitAsync(new NewWorkflowInboxMessage
            {
                ActivityTypeName = activityDescriptor.TypeName,
                BookmarkPayload = bookmarkPayloadWithCallControl,
                WorkflowInstanceId = workflowInstanceId,
                ActivityInstanceId = activityInstanceId,
                Input = input
            }, cancellationToken);
        }
    }

    private IEnumerable<ActivityDescriptor> FindActivityDescriptors(string eventType) =>
        _activityRegistry.FindMany(descriptor => descriptor.GetAttribute<WebhookDrivenAttribute>()?.EventTypes.Contains(eventType) == true);
}