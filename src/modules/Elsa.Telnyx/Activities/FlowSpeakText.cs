using System.Runtime.CompilerServices;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities.Flowchart.Attributes;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
[FlowNode("Done", "Finished speaking", "Disconnected")]
public class FlowSpeakText : SpeakTextBase
{
    /// <inheritdoc />
    public FlowSpeakText([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask HandleDisconnected(ActivityExecutionContext context) => await context.CompleteActivityWithOutcomesAsync("Disconnected", "Done");

    /// <inheritdoc />
    protected override async ValueTask HandleSpeakingHasFinished(ActivityExecutionContext context) => await context.CompleteActivityWithOutcomesAsync("Finished speaking", "Done");
}