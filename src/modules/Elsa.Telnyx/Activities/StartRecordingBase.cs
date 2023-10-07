using Elsa.Extensions;
using Elsa.Telnyx.Attributes;
using Elsa.Telnyx.Bookmarks;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Telnyx.Payloads.Call;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Start recording the call.
/// </summary>
[Activity(Constants.Namespace, "Start recording the call.", Kind = ActivityKind.Task)]
[WebhookDriven(WebhookEventTypes.CallRecordingSaved)]
public abstract class StartRecordingBase : Activity<CallRecordingSavedPayload>
{
    /// <inheritdoc />
    protected StartRecordingBase(string? source = default, int? line = default) : base(source, line)
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
    /// When 'dual', final audio file will be stereo recorded with the first leg on channel A, and the rest on channel B.
    /// </summary>
    [Input(
        Description = "When 'dual', final audio file will be stereo recorded with the first leg on channel A, and the rest on channel B.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "single", "dual" },
        DefaultValue = "single"
    )]
    public Input<string> Channels { get; set; } = new("single");

    /// <summary>
    /// The audio file format used when storing the call recording. Can be either 'mp3' or 'wav'.
    /// </summary>
    [Input(
        Description = "The audio file format used when storing the call recording. Can be either 'mp3' or 'wav'.",
        UIHint = InputUIHints.Dropdown,
        Options = new[] { "wav", "mp3" },
        DefaultValue = "wav"
    )]
    public Input<string> Format { get; set; } = new("wav");

    /// <summary>
    /// If enabled, a beep sound will be played at the start of a recording.
    /// </summary>
    [Input(Description = "If enabled, a beep sound will be played at the start of a recording.")]
    public Input<bool?>? PlayBeep { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var request = new StartRecordingRequest(
            Channels.GetOrDefault(context) ?? "single",
            Format.GetOrDefault(context) ?? "wav",
            PlayBeep.GetOrDefault(context),
            ClientState: context.CreateCorrelatingClientState()
        );

        var callControlId = CallControlId.Get(context);
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.StartRecordingAsync(callControlId, request, context.CancellationToken);
            
            context.CreateBookmark(new WebhookEventBookmarkPayload(WebhookEventTypes.CallRecordingSaved, callControlId), ResumeAsync, false);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await HandleDisconnectedAsync(context);
        }
    }
    
    /// <summary>
    /// Called when the recording was saved.
    /// </summary>
    protected abstract ValueTask HandleCallRecordingSavedAsync(ActivityExecutionContext context);
    
    
    /// <summary>
    /// Called when the call was no longer active.
    /// </summary>
    protected abstract ValueTask HandleDisconnectedAsync(ActivityExecutionContext context);

    private async ValueTask ResumeAsync(ActivityExecutionContext context)
    {
        var payload = context.GetWorkflowInput<CallRecordingSavedPayload>();
        context.Set(Result, payload);
        await HandleCallRecordingSavedAsync(context);
    }
}