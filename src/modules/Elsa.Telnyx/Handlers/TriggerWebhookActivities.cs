using System.Reflection;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Telnyx.Activities;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Events;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Runtime.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Elsa.Telnyx.Handlers;

/// <summary>
/// Triggers all workflows starting with or blocked on a <see cref="WebhookEvent"/> activity.
/// </summary>
[PublicAPI]
internal class TriggerWebhookActivities : INotificationHandler<TelnyxWebhookReceived>
{
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly ILogger _logger;

    public TriggerWebhookActivities(IWorkflowRuntime workflowRuntime, ILogger<TriggerWebhookActivities> logger)
    {
        _workflowRuntime = workflowRuntime;
        _logger = logger;
    }

    public async Task HandleAsync(TelnyxWebhookReceived notification, CancellationToken cancellationToken)
    {
        var webhook = notification.Webhook;
        var eventType = webhook.Data.EventType;
        var payload = webhook.Data.Payload;
        var activityType = payload.GetType().GetCustomAttribute<WebhookAttribute>()?.ActivityType;

        if (activityType == null)
            return;

        var correlationId = ((Payload)webhook.Data.Payload).GetCorrelationId();
        var callControlId = (webhook.Data.Payload as CallPayload)?.CallControlId;
        var bookmarkPayloadWithCallControl = new WebhookEventBookmarkPayload(eventType, callControlId);
        var bookmarkPayload = new WebhookEventBookmarkPayload(eventType);
        var input = new Dictionary<string, object>().AddInput(webhook);
        var options = new TriggerWorkflowsRuntimeOptions(correlationId, default, input);
        
        await _workflowRuntime.TriggerWorkflowsAsync(activityType, bookmarkPayload, options, cancellationToken);
        await _workflowRuntime.TriggerWorkflowsAsync(activityType, bookmarkPayloadWithCallControl, options, cancellationToken);
    }
}