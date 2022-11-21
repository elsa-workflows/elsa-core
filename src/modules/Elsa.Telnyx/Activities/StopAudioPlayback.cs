using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Refit;

namespace Elsa.Telnyx.Activities;

[FlowNode("Playback ended", "Disconnected")]
public class FlowStopAudioPlayback : StopAudioPlaybackBase
{
    protected override ValueTask HandlePlaybackEndedAsync(ActivityExecutionContext context) => context.CompleteActivityWithOutcomesAsync("Playback ended");
    protected override ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => context.CompleteActivityWithOutcomesAsync("Disconnected");
}

public class StopAudioPlayback : StopAudioPlaybackBase
{
    [Port] public IActivity? PlaybackEnded { get; set; }
    [Port] public IActivity? Disconnected { get; set; }

    protected override async ValueTask HandlePlaybackEndedAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(PlaybackEnded, OnCompletedAsync);
    protected override async ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Disconnected, OnCompletedAsync);
}

/// <summary>
/// Stop audio playback.
/// </summary>
[Activity(Constants.Namespace, Description = "Stop audio playback.", Kind = ActivityKind.Task)]
[FlowNode("Playback ended", "Disconnected")]
public abstract class StopAudioPlaybackBase : ActivityBase
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
    /// Use 'current' to stop only the current audio or 'all' to stop all audios in the queue.
    /// </summary>
    [Input(
        Description = "Use 'current' to stop only the current audio or 'all' to stop all audios in the queue.",
        DefaultValue = "all",
        Category = "Advanced"
    )]
    public Input<string?> Stop { get; set; } = new("all");

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var request = new StopAudioPlaybackRequest(Stop.Get(context));
        var callControlId = context.GetPrimaryCallControlId(CallControlId) ?? throw new Exception("CallControlId is required.");
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.StopAudioPlaybackAsync(callControlId, request, context.CancellationToken);
            context.CreateBookmark(ResumeAsync);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await context.CompleteActivityWithOutcomesAsync("Disconnected");
        }
    }

    protected abstract ValueTask HandlePlaybackEndedAsync(ActivityExecutionContext context);
    protected abstract ValueTask HandleDisconnectedAsync(ActivityExecutionContext context);
    protected async ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => await context.CompleteActivityAsync();

    private async ValueTask ResumeAsync(ActivityExecutionContext context) => await context.CompleteActivityWithOutcomesAsync("Playback ended");
}