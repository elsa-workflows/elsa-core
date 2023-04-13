using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Helpers;
using Elsa.Telnyx.Models;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Represents a Telnyx webhook event trigger.
/// </summary>
[Activity("Telnyx", "Telnyx", "A Telnyx webhook event that executes when a webhook event is received.", Kind = ActivityKind.Trigger)]
[Browsable(false)]
public class WebhookEvent : Activity<Payload>
{
    /// <inheritdoc />
    [JsonConstructor]
    public WebhookEvent([CallerFilePath]string? source = default, [CallerLineNumber]int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public WebhookEvent(string eventType, string activityTypeName, Variable<Payload> result, int version = 1, [CallerFilePath]string? source = default, [CallerLineNumber]int? line = default) 
        : base(activityTypeName, version, source, line)
    {
        EventType = eventType;
        Result = new(result);
    }
    
    /// <summary>
    /// The Telnyx webhook event type to listen for.
    /// </summary>
    [Description("The Telnyx webhook event type to listen for")]
    public string EventType { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (context.IsTriggerOfWorkflow())
            await Resume(context);
        else
        {
            var eventType = EventType;
            var payload = new WebhookEventBookmarkPayload(eventType);
            
            context.CreateBookmark(new BookmarkOptions(payload, Resume, Type));
        }
    }

    private async ValueTask Resume(ActivityExecutionContext context)
    {
        var input = context.GetInput<TelnyxWebhook>(WebhookSerializerOptions.Create());
        context.Set(Result, input.Data.Payload);
        await CompleteAsync(context);
    }
}