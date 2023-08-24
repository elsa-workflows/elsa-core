using System.Runtime.CompilerServices;
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
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Play an audio file on the call.
/// </summary>
[Activity(Constants.Namespace, "Play an audio file on the call.", Kind = ActivityKind.Task)]
[FlowNode("Playback started", "Disconnected")]
[WebhookDriven(WebhookEventTypes.CallPlaybackStarted)]
public abstract class PlayAudioBase : Activity
{
    /// <inheritdoc />
    protected PlayAudioBase([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
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
    public Input<string> CallControlId { get; set; } = default!;

    /// <summary>
    /// The URL of a file to be played back at the beginning of each prompt. The URL can point to either a WAV or MP3 file.
    /// </summary>
    [Input(
        DisplayName = "Audio URL",
        Description = "The URL of a file to be played back at the beginning of each prompt. The URL can point to either a WAV or MP3 file."
    )]
    public Input<Uri> AudioUrl { get; set; } = default!;

    /// <summary>
    /// The number of times the audio file should be played. If supplied, the value must be an integer between 1 and 100, or the special string 'infinity' for an endless loop.
    /// </summary>
    [Input(
        Description = "The number of times the audio file should be played. If supplied, the value must be an integer between 1 and 100, or the special string 'infinity' for an endless loop.",
        DefaultValue = "1",
        Category = "Advanced"
    )]
    public Input<string?> Loop { get; set; } = new("1");

    /// <summary>
    /// When enabled, audio will be mixed on top of any other audio that is actively being played back. Note that `overlay: true` will only work if there is another audio file already being played on the call.
    /// </summary>
    [Input(
        Description = "When enabled, audio will be mixed on top of any other audio that is actively being played back. Note that `overlay: true` will only work if there is another audio file already being played on the call.",
        DefaultValue = false,
        Category = "Advanced"
    )]
    public Input<bool> Overlay { get; set; } = new(false);

    /// <summary>
    /// Specifies the leg or legs on which audio will be played. If supplied, the value must be either 'self', 'opposite' or 'both'.
    /// </summary>
    [Input(
        Description = "Specifies the leg or legs on which audio will be played. If supplied, the value must be either 'self', 'opposite' or 'both'.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "", "self", "opposite", "both" },
        Category = "Advanced"
    )]
    public Input<string?> TargetLegs { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var loop = Loop.GetOrDefault(context);

        var request = new PlayAudioRequest(
            AudioUrl.Get(context),
            Overlay.GetOrDefault(context),
            string.IsNullOrWhiteSpace(loop) ? null : loop == "infinity" ? "infinity" : int.Parse(loop),
            TargetLegs.GetOrDefault(context).EmptyToNull(),
            ClientState: context.CreateCorrelatingClientState(context.Id)
        );

        var callControlId = CallControlId.Get(context);
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.PlayAudioAsync(callControlId, request, context.CancellationToken);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await HandleDisconnectedAsync(context);
        }
        
        context.CreateBookmark(new WebhookEventBookmarkPayload(WebhookEventTypes.CallPlaybackStarted, callControlId), ResumeAsync, true);
    }

    /// <summary>
    /// Called when playback has started.
    /// </summary>
    protected abstract ValueTask HandlePlaybackStartedAsync(ActivityExecutionContext context);


    /// <summary>
    /// Called when the call was no longer active. 
    /// </summary>
    protected abstract ValueTask HandleDisconnectedAsync(ActivityExecutionContext context);

    private async ValueTask ResumeAsync(ActivityExecutionContext context) => await HandlePlaybackStartedAsync(context);
}