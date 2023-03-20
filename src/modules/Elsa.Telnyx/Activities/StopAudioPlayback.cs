using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Telnyx.Client.Models;
using Elsa.Telnyx.Client.Services;
using Elsa.Telnyx.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Refit;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
[FlowNode("Done", "Disconnected")]
public class FlowStopAudioPlayback : StopAudioPlaybackBase
{
    /// <inheritdoc />
    [JsonConstructor]
    public FlowStopAudioPlayback([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <inheritdoc />
    protected override ValueTask HandleDoneAsync(ActivityExecutionContext context) => context.CompleteActivityWithOutcomesAsync("Done");

    /// <inheritdoc />
    protected override ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => context.CompleteActivityWithOutcomesAsync("Disconnected");
}

/// <inheritdoc />
public class StopAudioPlayback : StopAudioPlaybackBase
{
    /// <inheritdoc />
    [JsonConstructor]
    public StopAudioPlayback([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// The <see cref="IActivity"/> to execute when the call was no longer active.
    /// </summary>
    [Port] public IActivity? Disconnected { get; set; }

    /// <inheritdoc />
    protected override async ValueTask HandleDoneAsync(ActivityExecutionContext context) => await context.CompleteActivityAsync();

    /// <inheritdoc />
    protected override async ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Disconnected, OnCompletedAsync);

    private async ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => await context.CompleteActivityAsync();
}

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

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var request = new StopAudioPlaybackRequest(Stop.Get(context), context.CreateCorrelatingClientState());
        var callControlId = context.GetPrimaryCallControlId(CallControlId) ?? throw new Exception("CallControlId is required.");
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