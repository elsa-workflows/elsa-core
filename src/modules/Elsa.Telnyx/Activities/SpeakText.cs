using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Models;
using Refit;

namespace Elsa.Telnyx.Activities;

[Activity(Constants.Namespace, "Convert text to speech and play it back on the call.", Kind = ActivityKind.Task)]
[WebhookDriven(WebhookEventTypes.CallSpeakEnded)]
public abstract class SpeakTextBase : ActivityBase
{
    /// <summary>
    /// Unique identifier and token for controlling the call.
    /// </summary>
    [Input(
        DisplayName = "Call Control ID",
        Description = "Unique identifier and token for controlling the call.",
        Category = "Advanced"
    )]
    public Input<string?> CallControlId { get; set; } = default!;

    /// <summary>
    /// The language you want spoken.
    /// </summary>
    [Input(
        Description = "The language you want spoken.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "en-US", "en-AU", "nl-NL", "es-ES", "ru-RU" },
        DefaultValue = "en-US"
    )]
    public Input<string> Language { get; set; } = new("en-US");

    /// <summary>
    /// The gender of the voice used to speak back the text.
    /// </summary>
    [Input(
        Description = "The gender of the voice used to speak back the text.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "female", "male" },
        DefaultValue = "female"
    )]
    public Input<string> Voice { get; set; } = new("female");

    /// <summary>
    /// The text or SSML to be converted into speech. There is a 5,000 character limit.
    /// </summary>
    [Input(
        Description = "The text or SSML to be converted into speech. There is a 5,000 character limit.",
        UIHint = InputUIHints.MultiLine
    )]
    public Input<string> Payload { get; set; } = default!;

    /// <summary>
    /// The type of the provided payload. The payload can either be plain text, or Speech Synthesis Markup Language (SSML).
    /// </summary>
    [Input(
        Description = "The type of the provided payload. The payload can either be plain text, or Speech Synthesis Markup Language (SSML).",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "", "text", "ssml" }
    )]
    public Input<string?>? PayloadType { get; set; }

    /// <summary>
    /// This parameter impacts speech quality, language options and payload types. When using `basic`, only the `en-US` language and payload type `text` are allowed.
    /// </summary>
    [Input(
        Description = "This parameter impacts speech quality, language options and payload types. When using `basic`, only the `en-US` language and payload type `text` are allowed.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "", "basic", "premium" },
        Category = "Advanced"
    )]
    public Input<string?> ServiceLevel { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var request = new SpeakTextRequest(
            Language.Get(context) ?? throw new Exception("Language is required."),
            Voice.Get(context) ?? throw new Exception("Voice is required"),
            Payload.Get(context) ?? throw new Exception("Payload is required"),
            PayloadType.Get(context).EmptyToNull(),
            ServiceLevel.Get(context).EmptyToNull()
        );

        var callControlId = context.GetPrimaryCallControlId(CallControlId) ?? throw new Exception("CallControlId is required.");
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.SpeakTextAsync(callControlId, request, context.CancellationToken);
            context.CreateBookmark(new WebhookEventBookmarkPayload(WebhookEventTypes.CallSpeakEnded), ResumeAsync);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await HandleDisconnected(context);
        }
    }

    protected abstract ValueTask HandleDisconnected(ActivityExecutionContext context);
    protected abstract ValueTask HandleFinishedSpeaking(ActivityExecutionContext context);

    private async ValueTask ResumeAsync(ActivityExecutionContext context) => await HandleFinishedSpeaking(context);
}

[FlowNode("Finished speaking", "Disconnected")]
public class FlowSpeakText : SpeakTextBase
{
    protected override async ValueTask HandleDisconnected(ActivityExecutionContext context) => await context.CompleteActivityWithOutcomesAsync("Disconnected");
    protected override async ValueTask HandleFinishedSpeaking(ActivityExecutionContext context) => await context.CompleteActivityWithOutcomesAsync("Finished speaking");
}

public class SpeakText : SpeakTextBase
{
    [Port]public IActivity? FinishedSpeaking { get; set; }
    [Port]public IActivity? Disconnected { get; set; }
    
    protected override async ValueTask HandleDisconnected(ActivityExecutionContext context)
    {
        await context.ScheduleActivityAsync(Disconnected);
    }

    protected override async ValueTask HandleFinishedSpeaking(ActivityExecutionContext context)
    {
        await context.ScheduleActivityAsync(FinishedSpeaking); 
    }
}