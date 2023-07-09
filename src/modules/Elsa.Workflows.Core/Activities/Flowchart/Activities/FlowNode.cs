using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities.Flowchart.Activities;

/// <summary>
/// A simple container that executes the specified activity.
/// </summary>
[Activity("Elsa", "Flow", "A simple container that executes the specified activity.")]
[PublicAPI]
public class FlowNode : Activity
{
    /// <inheritdoc />
    [JsonConstructor]
    public FlowNode()
    {
    }
    
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

    private async ValueTask OnBodyCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => await context.CompleteActivityAsync();
}