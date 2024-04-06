using System.Runtime.CompilerServices;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Activities.Flowchart.Models;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
[FlowNode("Connected", "Disconnected")]
public class FlowAnswerCall : AnswerCallBase
{
    /// <inheritdoc />
    public FlowAnswerCall([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask HandleConnectedAsync(ActivityExecutionContext context) => await context.CompleteActivityAsync(new Outcomes("Connected"));

    /// <inheritdoc />
    protected override async ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => await context.CompleteActivityAsync(new Outcomes("Disconnected"));
}