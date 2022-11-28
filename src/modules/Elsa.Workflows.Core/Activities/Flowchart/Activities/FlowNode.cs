using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

/// <summary>
/// A simple container that executes the specified activity.
/// </summary>
[Activity("Elsa", "Flow", "A simple container that executes the specified activity.")]
public class FlowNode : ActivityBase
{
    /// <summary>
    /// The activity to execute.
    /// </summary>
    [Port]
    public IActivity? Body { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context) => await context.ScheduleActivityAsync(Body, OnBodyCompletedAsync);

    private async ValueTask OnBodyCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => await context.CompleteActivityAsync();
}