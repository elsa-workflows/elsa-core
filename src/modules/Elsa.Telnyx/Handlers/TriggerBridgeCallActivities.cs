using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Telnyx.Handlers;

/// <summary>
/// Triggers all workflows starting with or blocked on a <see cref="IncomingCall"/> activity.
/// </summary>
internal class TriggerBridgeCallActivities : INotificationHandler<TelnyxWebhookReceived>
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly ILogger _logger;

    public TriggerBridgeCallActivities(IWorkflowRuntime workflowRuntime, ILogger<TriggerBridgeCallActivities> logger)
    {
        _workflowRuntime = workflowRuntime;
        _logger = logger;
    }

    public async Task HandleAsync(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
    {
        var webhook = notification.Webhook;
        var payload = webhook.Data.Payload;

        if (payload is not CallBridgedPayload callBridgedPayload)
            return;
        
        var correlationId = ((Payload)webhook.Data.Payload).GetCorrelationId();;
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<BridgeCalls>();
        var input = new Dictionary<string, object>().AddInput(callBridgedPayload);
        var callBridgedBookmarkPayload = new CallBridgedBookmarkPayload(callBridgedPayload.CallControlId);
        await _workflowRuntime.TriggerWorkflowsAsync(activityTypeName, callBridgedBookmarkPayload, new TriggerWorkflowsRuntimeOptions(correlationId, default, input), cancellationToken);
    }
}