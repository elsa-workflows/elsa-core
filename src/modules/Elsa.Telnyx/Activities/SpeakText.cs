using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Extensions;
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

/// <summary>
/// Convert text to speech and play it back on the call.
/// </summary>
[Activity(Constants.Namespace, "Convert text to speech and play it back on the call.", Kind = ActivityKind.Task)]
public abstract class SpeakTextBase : Activity
{
    /// <inheritdoc />
    protected SpeakTextBase([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

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
            Language.Get(context),
            Voice.Get(context),
            Payload.Get(context),
            PayloadType.TryGet(context).EmptyToNull(),
            ServiceLevel.TryGet(context).EmptyToNull(),
            ClientState: context.CreateCorrelatingClientState()
        );

        var callControlId = context.GetPrimaryCallControlId(CallControlId) ?? throw new Exception("CallControlId is required.");
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.SpeakTextAsync(callControlId, request, context.CancellationToken);
            await HandleDone(context);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await HandleDisconnected(context);
        }
    }

    /// <summary>
    /// Called when the call was no longer active.
    /// </summary>
    protected abstract ValueTask HandleDisconnected(ActivityExecutionContext context);

    /// <summary>
    /// Called when speaking has finished.
    /// </summary>
    protected abstract ValueTask HandleDone(ActivityExecutionContext context);

    private async ValueTask ResumeAsync(ActivityExecutionContext context) => await HandleDone(context);
}

/// <inheritdoc />
[FlowNode("Done", "Finished speaking", "Disconnected")]
public class FlowSpeakText : SpeakTextBase
{
    /// <inheritdoc />
    [JsonConstructor]
    public FlowSpeakText([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask HandleDisconnected(ActivityExecutionContext context) => await context.CompleteActivityWithOutcomesAsync("Disconnected", "Done");

    /// <inheritdoc />
    protected override async ValueTask HandleDone(ActivityExecutionContext context) => await context.CompleteActivityWithOutcomesAsync("Finished speaking", "Done");
}

/// <inheritdoc />
public class SpeakText : SpeakTextBase
{
    /// <inheritdoc />
    [JsonConstructor]
    public SpeakText([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The <see cref="IActivity"/> to execute when the call was no longer active.
    /// </summary>
    [Port]
    public IActivity? Disconnected { get; set; }
    
    /// <summary>
    /// The <see cref="IActivity"/> to execute when speaking has finished.
    /// </summary>
    [Port]
    public IActivity? FinishedSpeaking { get; set; }

    /// <inheritdoc />
    protected override async ValueTask HandleDisconnected(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Disconnected);

    /// <inheritdoc />
    protected override async ValueTask HandleDone(ActivityExecutionContext context) => await context.ScheduleActivityAsync(FinishedSpeaking);
}