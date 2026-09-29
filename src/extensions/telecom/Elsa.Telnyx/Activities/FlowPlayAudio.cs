using System.Runtime.CompilerServices;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
[FlowNode("Playback started", "Disconnected")]
public class FlowPlayAudio : PlayAudioBase
{
    /// <inheritdoc />
    public FlowPlayAudio([CallerFilePath] string? source = null, [CallerLineNumber] int? line = null) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected override ValueTask HandlePlaybackStartedAsync(ActivityExecutionContext context) => context.CompleteActivityWithOutcomesAsync("Playback started");

    /// <inheritdoc />
    protected override ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => context.CompleteActivityWithOutcomesAsync("Disconnected");
}