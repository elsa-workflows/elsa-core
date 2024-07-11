using System.Runtime.CompilerServices;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;

namespace Elsa.Telnyx.Activities;

/// <inheritdoc />
[FlowNode("Bridged", "Disconnected")]
public class FlowBridgeCalls : BridgeCallsBase
{
    /// <inheritdoc />
    public FlowBridgeCalls([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected override ValueTask HandleDisconnectedAsync(ActivityExecutionContext context) => context.CompleteActivityAsync("Disconnected");

    /// <inheritdoc />
    protected override ValueTask HandleBridgedAsync(ActivityExecutionContext context) => context.CompleteActivityAsync("Bridged");
}