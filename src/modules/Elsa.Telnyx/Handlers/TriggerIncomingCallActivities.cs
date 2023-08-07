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

        if (callInitiatedPayload.Direction != "incoming")
            return;

        var correlationId = callInitiatedPayload.GetCorrelationId();
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<IncomingCall>();
        var input = new Dictionary<string, object>().AddInput(webhook);

        // Trigger all workflows matching the From number.
        var fromNumber = callInitiatedPayload.From;
        var fromBookmarkPayload = new IncomingCallFromBookmarkPayload(fromNumber);

        await _workflowInbox.SubmitAsync(new NewWorkflowInboxMessage
        {
            ActivityTypeName = activityTypeName,
            BookmarkPayload = fromBookmarkPayload,
            CorrelationId = correlationId,
            Input = input
        }, cancellationToken);

        // Trigger all workflows matching the To number.
        var toNumber = callInitiatedPayload.To;
        var toBookmarkPayload = new IncomingCallToBookmarkPayload(toNumber);
        await _workflowInbox.SubmitAsync(new NewWorkflowInboxMessage
        {
            ActivityTypeName = activityTypeName,
            BookmarkPayload = toBookmarkPayload,
            CorrelationId = correlationId,
            Input = input
        }, cancellationToken);

        // Trigger all catch-all workflows.
        var catchallBookmarkPayload = new IncomingCallCatchAllBookmarkPayload();
        await _workflowInbox.SubmitAsync(new NewWorkflowInboxMessage
        {
            ActivityTypeName = activityTypeName,
            BookmarkPayload = catchallBookmarkPayload,
            CorrelationId = correlationId,
            Input = input
        }, cancellationToken);
    }
}