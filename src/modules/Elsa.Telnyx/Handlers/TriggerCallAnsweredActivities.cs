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
internal class TriggerCallAnsweredActivities(IStimulusSender stimulusSender, ILogger<TriggerCallAnsweredActivities> logger) : INotificationHandler<TelnyxWebhookReceived>
{
    private readonly ILogger _logger = logger;

    public async Task HandleAsync(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
    {
        var webhook = notification.Webhook;
        var payload = webhook.Data.Payload;

        if (payload is not CallAnsweredPayload callAnsweredPayload)
            return;

        var clientStatePayload = callAnsweredPayload.GetClientStatePayload();
        var workflowInstanceId = clientStatePayload?.WorkflowInstanceId;
        var activityInstanceId = clientStatePayload?.ActivityInstanceId!;
        var input = new Dictionary<string, object>().AddInput(callAnsweredPayload);
        var callControlId = callAnsweredPayload.CallControlId;

        var stimulus = new CallAnsweredStimulus(callControlId);
        var metadata = new StimulusMetadata
        {
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId,
            Input = input
        };
        await stimulusSender.SendAsync<CallAnswered>(stimulus, metadata, cancellationToken);
    }
}