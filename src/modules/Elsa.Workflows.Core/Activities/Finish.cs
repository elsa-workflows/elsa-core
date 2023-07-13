using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Attributes;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Mark the workflow as finished.
/// </summary>
[Activity("Elsa", "Primitives", "Mark the workflow as finished.")]
[PublicAPI]
public class Finish : Activity
{
    /// <inheritdoc />
    [JsonConstructor]
    public Finish()
    {
    }
    
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