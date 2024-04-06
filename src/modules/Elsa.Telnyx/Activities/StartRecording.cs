using System.Runtime.CompilerServices;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using JetBrains.Annotations;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
[PublicAPI]
public class StartRecording : StartRecordingBase
{
    /// <inheritdoc />
    public StartRecording([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// The <see cref="IActivity"/> to execute when recording has finished.
    /// </summary>
    [Port] public IActivity? RecordingFinished { get; set; }
    
    /// <summary>
    /// The <see cref="IActivity"/> to executed when the call was no longer active.
    /// </summary>
    [Port] public IActivity? Disconnected { get; set; }

    /// <inheritdoc />
    protected override async ValueTask HandleCallRecordingSavedAsync(ActivityExecutionContext context)  => await context.ScheduleActivityAsync(RecordingFinished, OnCompletedAsync);

    /// <inheritdoc />
    protected override async ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Disconnected, OnCompletedAsync);

    private async ValueTask OnCompletedAsync(ActivityCompletedContext context) => await context.TargetContext.CompleteActivityAsync();
}