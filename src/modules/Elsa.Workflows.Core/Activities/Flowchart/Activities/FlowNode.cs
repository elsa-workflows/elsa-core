using System.Runtime.CompilerServices;
using Elsa.Workflows.Attributes;
using JetBrains.Annotations;

namespace Elsa.Workflows.Activities.Flowchart.Activities;

/// <summary>
/// A simple container that executes the specified activity.
/// </summary>
[Activity("Elsa", "Flow", "A simple container that executes the specified activity.", DisplayName = "Container")]
[PublicAPI]
public class FlowNode : Activity
{
    
    /// <inheritdoc />
    public FlowNode([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <summary>
    /// The activity to execute.
    /// </summary>
    [Port]
    public IActivity? Body { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Body, OnBodyCompletedAsync);

    private async ValueTask OnBodyCompletedAsync(ActivityCompletedContext context) => await context.TargetContext.CompleteActivityAsync();
}