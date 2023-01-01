using Elsa.Extensions;
using Elsa.Mediator.Services;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Logging;

namespace Elsa.Telnyx.Handlers;

/// <summary>
/// Triggers all workflows starting with or blocked on a <see cref="IncomingCall"/> activity.
/// </summary>
internal class TriggerIncomingCallActivities : INotificationHandler<TelnyxWebhookReceived>
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly ILogger _logger;

    public TriggerIncomingCallActivities(IWorkflowRuntime workflowRuntime, ILogger<TriggerIncomingCallActivities> logger)
    {
        _workflowRuntime = workflowRuntime;
        _logger = logger;
    }

    public async Task HandleAsync(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
    {
        var webhook = notification.Webhook;
        var payload = webhook.Data.Payload;

        if (payload is not CallInitiatedPayload callInitiatedPayload)
            return;

        if (callInitiatedPayload.Direction != "incoming")
            return;

        var correlationId = ((Payload)webhook.Data.Payload).GetCorrelationId();;
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<IncomingCall>();
        var input = new Dictionary<string, object>().AddInput(webhook);

        // Trigger all workflows matching the From number.
        var fromNumber = callInitiatedPayload.From;
        var fromBookmarkPayload = new IncomingCallFromBookmarkPayload(fromNumber);
        await _workflowRuntime.TriggerWorkflowsAsync(activityTypeName, fromBookmarkPayload, new TriggerWorkflowsRuntimeOptions(correlationId, input), cancellationToken);

        // Trigger all workflows matching the To number.
        var toNumber = callInitiatedPayload.To;
        var toBookmarkPayload = new IncomingCallToBookmarkPayload(toNumber);
        await _workflowRuntime.TriggerWorkflowsAsync(activityTypeName, toBookmarkPayload, new TriggerWorkflowsRuntimeOptions(correlationId, input), cancellationToken);

        // Trigger all catch-all workflows.
        var catchallBookmarkPayload = new IncomingCallCatchAllBookmarkPayload();
        await _workflowRuntime.TriggerWorkflowsAsync(activityTypeName, catchallBookmarkPayload, new TriggerWorkflowsRuntimeOptions(correlationId, input), cancellationToken);
    }
}