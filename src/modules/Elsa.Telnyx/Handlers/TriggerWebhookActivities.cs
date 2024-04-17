using System.Reflection;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Abstractions;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Telnyx.Handlers;

/// <summary>
/// Triggers all workflows starting with or blocked on a <see cref="WebhookEvent"/> activity.
/// </summary>
[PublicAPI]
internal class TriggerWebhookActivities(IStimulusSender stimulusSender) : INotificationHandler<TelnyxWebhookReceived>
{
    public async Task HandleAsync(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
    {
        var webhook = notification.Webhook;
        var eventType = webhook.Data.EventType;
        var payload = webhook.Data.Payload;
        var activityType = payload.GetType().GetCustomAttribute<WebhookActivityAttribute>()?.ActivityType;

        if (activityType == null)
            return;

        var workflowInstanceId = ((Payload)webhook.Data.Payload).GetClientStatePayload()?.WorkflowInstanceId;
        var callControlId = (webhook.Data.Payload as CallPayload)?.CallControlId;
        var stimulus = new WebhookEventStimulus(eventType, callControlId);
        var input = new Dictionary<string, object>().AddInput(webhook);

        var metadata = new StimulusMetadata
        {
            Input = input,
            WorkflowInstanceId = workflowInstanceId,

        };
        await stimulusSender.SendAsync(activityType, stimulus, metadata, cancellationToken);
    }
}