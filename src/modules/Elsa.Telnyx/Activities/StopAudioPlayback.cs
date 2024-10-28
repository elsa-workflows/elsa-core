using System.Runtime.CompilerServices;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
public class StopAudioPlayback : StopAudioPlaybackBase
{
    /// <inheritdoc />
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

    private async ValueTask OnCompletedAsync(ActivityCompletedContext context) => await context.TargetContext.CompleteActivityAsync();
}