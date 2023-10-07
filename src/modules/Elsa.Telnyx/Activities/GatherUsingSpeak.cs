using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Convert text to speech and play it on the call until the required DTMF signals are gathered to build interactive menus.
/// </summary>
[Activity(Constants.Namespace, "Convert text to speech and play it on the call until the required DTMF signals are gathered to build interactive menus.", Kind = ActivityKind.Task)]
[FlowNode("Valid input", "Invalid input", "Disconnected")]
[WebhookDriven(WebhookEventTypes.CallGatherEnded)]
public class GatherUsingSpeak : Activity<CallGatherEndedPayload>
{
    /// <inheritdoc />
    public GatherUsingSpeak([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// The call control ID of the call from which to gather input. Leave empty to use the ambient call control ID, if there is any.
    /// </summary>
    [Input(DisplayName = "Call Control ID", Description = "The call control ID of the call from which to gather input. Leave empty to use the ambient call control ID, if there is any.", Category = "Advanced")]
    public Input<string> CallControlId { get; set; } = default!;
        
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
    /// "This parameter impacts speech quality, language options and payload types. When using `basic`, only the `en-US` language and payload type `text` are allowed."
    /// </summary>
    [Input(
        Description = "This parameter impacts speech quality, language options and payload types. When using `basic`, only the `en-US` language and payload type `text` are allowed.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "", "basic", "premium" },
        Category = "Advanced"
    )]
    public Input<string?>? ServiceLevel { get; set; }
        
    /// <summary>
    /// The number of milliseconds to wait for input between digits.
    /// </summary>
    [Input(
        DisplayName = "Inter Digit Timeout",
        Description = "The number of milliseconds to wait for input between digits.",
        Category = "Advanced",
        DefaultValue = 5000
    )]
    public Input<int?>? InterDigitTimeoutMillis { get; set; } = new(5000);

    /// <summary>
    /// A list of all digits accepted as valid.
    /// </summary>
    [Input(
        Description = "A list of all digits accepted as valid.",
        Category = "Advanced",
        DefaultValue = "0123456789#*"
    )]
    public Input<string?>? ValidDigits { get; set; } = new("0123456789#*");

    /// <summary>
    /// The minimum number of digits to fetch. This parameter has a minimum value of 1.
    /// </summary>
    [Input(Description = "The minimum number of digits to fetch. This parameter has a minimum value of 1.", DefaultValue = 1)]
    public Input<int?>? MinimumDigits { get; set; } = new(1);

    /// <summary>
    /// The maximum number of digits to fetch. This parameter has a maximum value of 128.
    /// </summary>
    [Input(Description = "The maximum number of digits to fetch. This parameter has a maximum value of 128.", DefaultValue = 128)]
    public Input<int?>? MaximumDigits { get; set; } = new(128);

    /// <summary>
    /// The maximum number of times the file should be played if there is no input from the user on the call.
    /// </summary>
    [Input(Description = "The maximum number of times the file should be played if there is no input from the user on the call.", DefaultValue = 3)]
    public Input<int?>? MaximumTries { get; set; } = new(3);

    /// <summary>
    /// The digit used to terminate input if fewer than `maximum_digits` digits have been gathered.
    /// </summary>
    [Input(Description = "The digit used to terminate input if fewer than `maximum_digits` digits have been gathered.", DefaultValue = "#")]
    public Input<string?>? TerminatingDigit { get; set; } = new("#");

    /// <summary>
    /// The number of milliseconds to wait for a DTMF response after file playback ends before a replaying the sound file.
    /// </summary>
    [Input(
        DisplayName = "Timeout",
        Description = "The number of milliseconds to wait for a DTMF response after file playback ends before a replaying the sound file.",
        Category = "Advanced",
        DefaultValue = 60000
    )]
    public Input<int?>? TimeoutMillis { get; set; } = new(60000);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var callControlId = CallControlId.Get(context);
        
        var request = new GatherUsingSpeakRequest(
            Language.Get(context) ?? throw new Exception("Language is required."),
            Voice.Get(context) ?? throw new Exception("Voice is required."),
            Payload.Get(context) ?? throw new Exception("Payload is required."),
            PayloadType.GetOrDefault(context),
            ServiceLevel.GetOrDefault(context),
            InterDigitTimeoutMillis.GetOrDefault(context),
            MaximumDigits.GetOrDefault(context),
            MaximumTries.GetOrDefault(context),
            MinimumDigits.GetOrDefault(context),
            TerminatingDigit.GetOrDefault(context).EmptyToNull(),
            TimeoutMillis.GetOrDefault(context),
            ValidDigits.GetOrDefault(context).EmptyToNull(),
            context.CreateCorrelatingClientState(context.Id)
        );
        
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            // Send the request to Telnyx.
            await telnyxClient.Calls.GatherUsingSpeakAsync(callControlId, request, context.CancellationToken);
            
            // Create a bookmark so we can resume this activity when the call.gather.ended webhook comes back.
            context.CreateBookmark(new WebhookEventBookmarkPayload(WebhookEventTypes.CallGatherEnded, callControlId), ResumeAsync, true);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await context.CompleteActivityWithOutcomesAsync("Disconnected");
        }
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var payload = context.GetWorkflowInput<CallGatherEndedPayload>();
        var outcome = payload.Status == "valid" ? "Valid input" : "Invalid input";
        context.Set(Result, payload);
        await context.CompleteActivityWithOutcomesAsync(outcome);
    }
}