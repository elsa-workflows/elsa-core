using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;

namespace Elsa.Telnyx.Handlers;

/// <summary>
/// Triggers all workflows starting with or blocked on a <see cref="CallHangup"/> activity.
/// </summary>
[PublicAPI]
internal class TriggerCallHangupActivities(IStimulusSender stimulusSender)
    : INotificationHandler<TelnyxWebhookReceived>
{
    public async Task HandleAsync(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
    {
        var webhook = notification.Webhook;
        var payload = webhook.Data.Payload;

        if (payload is not CallHangupPayload callHangupPayload)
            return;

        var clientStatePayload = callHangupPayload.GetClientStatePayload();
        var workflowInstanceId = clientStatePayload?.WorkflowInstanceId;
        var input = new Dictionary<string, object>().AddInput(callHangupPayload);
        var callControlId = callHangupPayload.CallControlId;
        var stimulus = new CallHangupStimulus(callControlId);
        var metadata = new StimulusMetadata
        {
            WorkflowInstanceId = workflowInstanceId,
            Input = input
        };
        await stimulusSender.SendAsync<CallHangup>(stimulus, metadata, cancellationToken);
    }
}