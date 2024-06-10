using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Telnyx.Handlers;

/// <summary>
/// Triggers all workflows starting with or blocked on a <see cref="CallAnswered"/> activity.
/// </summary>
[PublicAPI]
internal class TriggerCallBridgedActivities(IStimulusSender stimulusSender) : INotificationHandler<TelnyxWebhookReceived>
{
    public async Task HandleAsync(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
    {
        var webhook = notification.Webhook;
        var payload = webhook.Data.Payload;

        if (payload is not CallBridgedPayload callBridgedPayload)
            return;

        var clientStatePayload = callBridgedPayload.GetClientStatePayload();
        var workflowInstanceId = clientStatePayload?.WorkflowInstanceId;
        var input = new Dictionary<string, object>().AddInput(callBridgedPayload);
        var callControlId = callBridgedPayload.CallControlId;
        var activityTypeNames = new[]
        {
            ActivityTypeNameHelper.GenerateTypeName<BridgeCalls>(),
            ActivityTypeNameHelper.GenerateTypeName<FlowBridgeCalls>(),
        };

        foreach (var activityTypeName in activityTypeNames)
        {
            var stimulus = new WebhookEventStimulus(WebhookEventTypes.CallBridged, callControlId);
            var metadata = new StimulusMetadata
            {
                WorkflowInstanceId = workflowInstanceId,
                Input = input
            };
            await stimulusSender.SendAsync(activityTypeName, stimulus, metadata, cancellationToken);
        }
    }
}