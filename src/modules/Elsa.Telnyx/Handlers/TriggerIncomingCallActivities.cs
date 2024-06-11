using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Runtime;
using JetBrains.Annotations;

namespace Elsa.Telnyx.Handlers;

/// <summary>
/// Triggers all workflows starting with or blocked on a <see cref="IncomingCall"/> activity.
/// </summary>
[PublicAPI]
internal class TriggerIncomingCallActivities(IStimulusSender stimulusSender)
    : INotificationHandler<TelnyxWebhookReceived>
{
    public async Task HandleAsync(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
    {
        var webhook = notification.Webhook;
        var payload = webhook.Data.Payload;

        if (payload is not CallInitiatedPayload callInitiatedPayload)
            return;

        // Only trigger workflows for incoming calls.
        if (callInitiatedPayload.Direction != "incoming" || callInitiatedPayload.ClientState != null)
            return;

        var input = new Dictionary<string, object>().AddInput(webhook);
        var stimulusMetadata = new StimulusMetadata
        {
            Input = input
        };

        // Trigger all workflows matching the From number.
        var fromNumber = callInitiatedPayload.From;
        var fromStimulus = new IncomingCallFromStimulus(fromNumber);
        var fromResults = await stimulusSender.SendAsync<IncomingCall>(fromStimulus, stimulusMetadata, cancellationToken);

        // Trigger all workflows matching the To number.
        var toNumber = callInitiatedPayload.To;
        var toStimulus = new IncomingCallToStimulus(toNumber);
        var toResults = await stimulusSender.SendAsync<IncomingCall>(toStimulus, stimulusMetadata, cancellationToken);

        // If any workflows were triggered, don't trigger the catch-all workflows.
        if (fromResults.WorkflowInstanceResponses.Any() || toResults.WorkflowInstanceResponses.Any())
            return;

        // Trigger all catch-all workflows.
        var catchAllStimulus = new IncomingCallCatchAllStimulus();
        await stimulusSender.SendAsync<IncomingCall>(catchAllStimulus, stimulusMetadata, cancellationToken);
    }
}