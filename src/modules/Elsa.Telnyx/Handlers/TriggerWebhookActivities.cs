using System.Reflection;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Abstractions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Telnyx.Handlers;

/// <summary>
/// Triggers all workflows starting with or blocked on a <see cref="WebhookEvent"/> activity.
/// </summary>
[PublicAPI]
internal class TriggerWebhookActivities : INotificationHandler<TelnyxWebhookReceived>
{
    private readonly IWorkflowInbox _workflowInbox;
    private readonly ILogger _logger;

    public TriggerWebhookActivities(IWorkflowInbox workflowInbox, ILogger<TriggerWebhookActivities> logger)
    {
        _workflowInbox = workflowInbox;
        _logger = logger;
    }

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
        var bookmarkPayloadWithCallControl = new WebhookEventBookmarkPayload(eventType, callControlId);
        var input = new Dictionary<string, object>().AddInput(webhook);
        
        await _workflowInbox.SubmitAsync(new NewWorkflowInboxMessage
        {
            ActivityTypeName = activityType,
            BookmarkPayload = bookmarkPayloadWithCallControl,
            WorkflowInstanceId = workflowInstanceId,
            Input = input
        }, cancellationToken);
    }
}