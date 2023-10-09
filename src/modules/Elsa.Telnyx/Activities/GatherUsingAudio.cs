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
using Elsa.Workflows.Runtime.Contracts;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Play an audio file on the call until the required DTMF signals are gathered to build interactive menus.
/// </summary>
[Activity(Constants.Namespace, "Play an audio file on the call until the required DTMF signals are gathered to build interactive menus.", Kind = ActivityKind.Task)]
[FlowNode("Valid input", "Invalid input", "Disconnected")]
[WebhookDriven(WebhookEventTypes.CallGatherEnded)]
public class GatherUsingAudio : Activity<CallGatherEndedPayload>, IBookmarksPersistedHandler
{
    /// <inheritdoc />
    public GatherUsingAudio([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// The call control ID of the call from which to gather input. Leave empty to use the ambient call control ID, if there is any.
    /// </summary>
    [Input(DisplayName = "Call Control ID", Description = "The call control ID of the call from which to gather input. Leave empty to use the ambient call control ID, if there is any.", Category = "Advanced")]
    public Input<string> CallControlId { get; set; } = default!;

    /// <summary>
    /// The URL of a file to be played back at the beginning of each prompt. The URL can point to either a WAV or MP3 file.
    /// </summary>
    [Input(
        DisplayName = "Audio URL",
        Description = "The URL of a file to be played back at the beginning of each prompt. The URL can point to either a WAV or MP3 file."
    )]
    public Input<Uri?>? AudioUrl { get; set; } = default!;

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
    /// The URL of a file to play when digits don't match the Valid Digits setting or the number of digits is not between Min and Max. The URL can point to either a WAV or MP3 file.
    /// </summary>
    [Input(
        DisplayName = "Invalid Audio Url",
        Description = "The URL of a file to play when digits don't match the Valid Digits setting or the number of digits is not between Min and Max. The URL can point to either a WAV or MP3 file."
    )]
    public Input<Uri?>? InvalidAudioUrl { get; set; }

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
    
    /// <summary>
    /// Calls out to Telnyx to actually begin gathering input.
    /// </summary>
    public async ValueTask BookmarksPersistedAsync(ActivityExecutionContext context)
    {
        var request = new GatherUsingAudioRequest(
            AudioUrl.Get(context) ?? throw new Exception("AudioUrl is required."),
            context.CreateCorrelatingClientState(),
            default,
            InterDigitTimeoutMillis.Get(context),
            InvalidAudioUrl.Get(context),
            MaximumDigits.Get(context),
            MaximumTries.Get(context),
            MinimumDigits.Get(context),
            TerminatingDigit.Get(context).EmptyToNull(),
            TimeoutMillis.Get(context),
            ValidDigits.Get(context).EmptyToNull()
        );

        var callControlId = CallControlId.Get(context);
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.GatherUsingAudioAsync(callControlId, request, context.CancellationToken);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await context.CompleteActivityWithOutcomesAsync("Disconnected");
        }
    }

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var callControlId = CallControlId.Get(context);
        context.CreateBookmark(new WebhookEventBookmarkPayload(WebhookEventTypes.CallGatherEnded, callControlId), ResumeAsync);
    }

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var payload = context.GetWorkflowInput<CallGatherEndedPayload>();
        var outcome = payload.Status == "valid" ? "Valid input" : "Invalid input";
        context.Set(Result, payload);
        await context.CompleteActivityWithOutcomesAsync(outcome);
    }
}