using System.Runtime.CompilerServices;
using Elsa.Workflows.Attributes;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities;

/// <summary>
/// Mark the workflow as finished.
/// </summary>
[Activity("Elsa", "Primitives", "Mark the workflow as finished.")]
[PublicAPI]
public class Finish : CodeActivity, ITerminalNode
{
    /// <inheritdoc />
    public Finish([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        context.ClearCompletionCallbacks();
        context.WorkflowExecutionContext.Scheduler.Clear();
        context.WorkflowExecutionContext.Bookmarks.Clear();
        context.WorkflowExecutionContext.TransitionTo(WorkflowSubStatus.Finished);
    }
}