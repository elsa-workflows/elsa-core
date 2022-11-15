using System.ComponentModel;
using Elsa.Telnyx.Bookmarks;
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
public class WebhookEvent : ActivityBase
{
    /// <summary>
    /// The Telnyx webhook event type to listen for.
    /// </summary>
    [Input(Description = "The Telnyx webhook event type to listen for")]
    public Input<string> EventType { get; set; } = default!;
    
    /// <summary>
    /// The received webhook payload.
    /// </summary>
    [Output] public Payload? Payload { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (!context.IsFirstPass())
        {
            var eventType = context.Get(EventType)!;
            var payload = new WebhookEventBookmarkPayload(eventType);
            context.CreateBookmark(payload, Resume);
        }
        else
            await Resume(context);
    }

    private async ValueTask Resume(ActivityExecutionContext context)
    {
        var input = context.GetInput<Payload>();
        Payload = input;
        await CompleteAsync(context);
    }
}