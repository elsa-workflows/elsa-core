using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Abstractions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;

namespace Elsa.Telnyx.Handlers;

/// <summary>
/// Resumes all workflows blocked on activities that are waiting for a given webhook.
/// </summary>
[PublicAPI]
internal class TriggerWebhookDrivenActivities(IStimulusSender stimulusSender, IActivityRegistry activityRegistry)
    : INotificationHandler<TelnyxWebhookReceived>
{
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
        var bookmarkPayloadWithCallControl = new WebhookEventStimulus(eventType, callControlId);

        foreach (var activityDescriptor in activityDescriptors)
        {
            var metadata = new StimulusMetadata
            {
                WorkflowInstanceId = workflowInstanceId,
                ActivityInstanceId = activityInstanceId,
                Input = input
            };
            
            await stimulusSender.SendAsync(activityDescriptor.TypeName, bookmarkPayloadWithCallControl, metadata, cancellationToken);
        }
    }

    private IEnumerable<ActivityDescriptor> FindActivityDescriptors(string eventType) =>
        activityRegistry.FindMany(descriptor => descriptor.GetAttribute<WebhookDrivenAttribute>()?.EventTypes.Contains(eventType) == true);
}