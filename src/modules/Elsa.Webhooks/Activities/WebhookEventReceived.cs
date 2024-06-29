using Elsa.Expressions.Helpers;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Webhooks.Stimuli;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using WebhooksCore;

namespace Elsa.Webhooks.Activities;

public class WebhookEventReceived : Trigger<WebhookEvent>
{
    /// The webhook event type to be received.
    [Input(Description = "The webhook event type to be received.")]
    public Input<string> EventType { get; set; } = default!;

    /// The webhook event payload type to be received.
    [Input(Description = "The webhook event payload type to be received.")]
    public Input<Type?> PayloadType { get; set; } = default!;

    /// The webhook payload, if any.
    [Output(Description = "The webhook payload, if any.")]
    public Output<object?> Payload { get; set; } = default!;

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (!context.IsTriggerOfWorkflow())
        {
            context.CreateBookmark(GetStimulus(context.ExpressionExecutionContext), OnResumeAsync);
            return;
        }

        await ExecuteInternalAsync(context);
    }

    protected override object GetTriggerPayload(TriggerIndexingContext context)
    {
        var eventType = EventType.Get(context.ExpressionExecutionContext);
        return new WebhookEventReceivedStimulus(eventType);
    }

    private async ValueTask OnResumeAsync(ActivityExecutionContext context)
    {
        await ExecuteInternalAsync(context);
    }

    private object GetStimulus(ExpressionExecutionContext context)
    {
        var eventType = EventType.Get(context);
        return new WebhookEventReceivedStimulus(eventType);
    }

    private async Task ExecuteInternalAsync(ActivityExecutionContext context)
    {
        var receivedWebhookEvent = context.GetWorkflowInput<WebhookEvent>();
        var payload = receivedWebhookEvent.Payload;
        var payloadType = PayloadType.GetOrDefault(context);

        if (payloadType != null && payload != null)
        {
            var deserializedPayload = payload.ConvertTo(payloadType);
            payload = deserializedPayload;
        }

        context.SetResult(receivedWebhookEvent);
        Payload.Set(context, payload);
        await context.CompleteActivityAsync();
    }
}