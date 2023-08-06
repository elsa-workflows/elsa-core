using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
[FlowNode("Recording finished", "Disconnected")]
public class FlowStartRecording : StartRecordingBase
{
    /// <inheritdoc />
    public FlowStartRecording([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <inheritdoc />
    protected override ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => context.CompleteActivityWithOutcomesAsync("Disconnected");

    /// <inheritdoc />
    protected override ValueTask HandleCallRecordingSavedAsync(ActivityExecutionContext context) => context.CompleteActivityWithOutcomesAsync("Recording finished");
}