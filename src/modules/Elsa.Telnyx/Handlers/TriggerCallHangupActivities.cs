using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Telnyx.Handlers;

/// <summary>
/// Triggers all workflows starting with or blocked on a <see cref="CallHangup"/> activity.
/// </summary>
[PublicAPI]
internal class TriggerCallHangupActivities : INotificationHandler<TelnyxWebhookReceived>
{
    private readonly IWorkflowInbox _workflowInbox;
    private readonly ILogger _logger;

    public TriggerCallHangupActivities(IWorkflowInbox workflowInbox, ILogger<TriggerCallHangupActivities> logger)
    {
        _workflowInbox = workflowInbox;
        _logger = logger;
    }

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
        
        await _workflowInbox.SubmitAsync(new NewWorkflowInboxMessage
        {
            ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<CallHangup>(),
            BookmarkPayload = new CallHangupBookmarkPayload(callControlId),
            WorkflowInstanceId = workflowInstanceId,
            Input = input
        }, cancellationToken);
    }
}