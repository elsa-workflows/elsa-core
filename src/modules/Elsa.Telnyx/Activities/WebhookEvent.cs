using System.ComponentModel;
using System.Text.Json.Serialization;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Helpers;
using Elsa.Telnyx.Models;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Represents a Telnyx webhook event trigger.
/// </summary>
[Activity("Telnyx", "Telnyx", "A Telnyx webhook event that executes when a webhook event is received.", Kind = ActivityKind.Trigger)]
[Browsable(false)]
public class WebhookEvent : ActivityBase<Payload>
{
    [JsonConstructor]
    public WebhookEvent()
    {
    }

    public WebhookEvent(string eventType, Variable<Payload> result)
    {
        EventType = new (eventType);
        Result = new(result);
    }
    
    /// <summary>
    /// The Telnyx webhook event type to listen for.
    /// </summary>
    [Input(Description = "The Telnyx webhook event type to listen for")]
    public Input<string> EventType { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (context.IsTriggerOfWorkflow())
            await Resume(context);
        else
        {
            var eventType = context.Get(EventType)!;
            var payload = new WebhookEventBookmarkPayload(eventType);
            context.CreateBookmark(payload, Resume);
        }
    }

    private async ValueTask Resume(ActivityExecutionContext context)
    {
        var input = context.GetInput<TelnyxWebhook>(WebhookSerializerOptions.Create());
        context.Set(Result, input.Data.Payload);
        await CompleteAsync(context);
    }
}