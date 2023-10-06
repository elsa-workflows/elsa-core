using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Telnyx.Handlers;

/// <summary>
/// Triggers all workflows starting with or blocked on a <see cref="IncomingCall"/> activity.
/// </summary>
[PublicAPI]
internal class TriggerIncomingCallActivities : INotificationHandler<TelnyxWebhookReceived>
{
    private readonly IWorkflowInbox _workflowInbox;
    private readonly ILogger _logger;

    public TriggerIncomingCallActivities(IWorkflowInbox workflowInbox, ILogger<TriggerIncomingCallActivities> logger)
    {
        _workflowInbox = workflowInbox;
        _logger = logger;
    }

    public async Task HandleAsync(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
    {
        var webhook = notification.Webhook;
        var payload = webhook.Data.Payload;

        if (payload is not CallInitiatedPayload callInitiatedPayload)
            return;

        // Only trigger workflows for incoming calls.
        if (callInitiatedPayload.Direction != "incoming" || callInitiatedPayload.ClientState != null)
            return;
        
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<IncomingCall>();
        var input = new Dictionary<string, object>().AddInput(webhook);

        // Trigger all workflows matching the From number.
        var fromNumber = callInitiatedPayload.From;
        var fromBookmarkPayload = new IncomingCallFromBookmarkPayload(fromNumber);

        var fromResults = await _workflowInbox.SubmitAsync(new NewWorkflowInboxMessage
        {
            ActivityTypeName = activityTypeName,
            BookmarkPayload = fromBookmarkPayload,
            Input = input
        }, cancellationToken);

        // Trigger all workflows matching the To number.
        var toNumber = callInitiatedPayload.To;
        var toBookmarkPayload = new IncomingCallToBookmarkPayload(toNumber);
        var toResults = await _workflowInbox.SubmitAsync(new NewWorkflowInboxMessage
        {
            ActivityTypeName = activityTypeName,
            BookmarkPayload = toBookmarkPayload,
            Input = input
        }, cancellationToken);

        // If any workflows were triggered, don't trigger the catch-all workflows.
        if (fromResults.WorkflowExecutionResults.Any() || toResults.WorkflowExecutionResults.Any())
            return;

        // Trigger all catch-all workflows.
        var catchallBookmarkPayload = new IncomingCallCatchAllBookmarkPayload();
        await _workflowInbox.SubmitAsync(new NewWorkflowInboxMessage
        {
            ActivityTypeName = activityTypeName,
            BookmarkPayload = catchallBookmarkPayload,
            Input = input
        }, cancellationToken);
    }
}