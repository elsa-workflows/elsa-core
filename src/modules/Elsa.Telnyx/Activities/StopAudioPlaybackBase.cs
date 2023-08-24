using Elsa.Extensions;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <summary>
/// Stop audio playback.
/// </summary>
[Activity(Constants.Namespace, Description = "Stop audio playback.", Kind = ActivityKind.Task)]
public abstract class StopAudioPlaybackBase : Activity
{
    /// <inheritdoc />
    protected StopAudioPlaybackBase(string? source = default, int? line = default) : base(source, line)
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
    /// Use 'current' to stop only the current audio or 'all' to stop all audios in the queue.
    /// </summary>
    [Input(
        Description = "Use 'current' to stop only the current audio or 'all' to stop all audios in the queue.",
        DefaultValue = "all",
        Category = "Advanced"
    )]
    public Input<string?> Stop { get; set; } = new("all");

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var request = new StopAudioPlaybackRequest(Stop.Get(context), context.CreateCorrelatingClientState(context.Id));
        var callControlId = CallControlId.Get(context);
        var telnyxClient = context.GetRequiredService<ITelnyxClient>();

        try
        {
            await telnyxClient.Calls.StopAudioPlaybackAsync(callControlId, request, context.CancellationToken);
            await HandleDoneAsync(context);
        }
        catch (ApiException e)
        {
            if (!await e.CallIsNoLongerActiveAsync()) throw;
            await HandleDisconnectedAsync(context);
        }
    }
    
    /// <summary>
    /// Called when audio playback is stopping.
    /// </summary>
    protected abstract ValueTask HandleDoneAsync(ActivityExecutionContext context);
    /// <summary>
    /// Called when the call was no longer active.
    /// </summary>
    protected abstract ValueTask HandleDisconnectedAsync(ActivityExecutionContext context);
}