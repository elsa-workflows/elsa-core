using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Represents a composite activity that has a single <see cref="Root"/> activity. Like a workflow, but without workflow-level properties.
/// </summary>
[Activity("Elsa", "Workflows", "Execute a root activity that you can configure yourself")]
public class Composite : Activity
{
    /// <summary>
    /// The activity to schedule when this activity executes.
    /// </summary>
    [Port]
    public IActivity Root { get; set; } = new Sequence();

    protected override void Execute(ActivityExecutionContext context)
    {
        context.ScheduleActivity(Root, OnCompletedAsync);
    }

    protected virtual ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => ValueTask.CompletedTask;

    protected static Inline From(Func<ActivityExecutionContext, ValueTask> activity) => new(activity);
    protected static Inline From(Func<ValueTask> activity) => new(activity);
    protected static Inline From(Action<ActivityExecutionContext> activity) => new(activity);
    protected static Inline From(Action activity) => new(activity);
    protected static Inline<TResult> From<TResult>(Func<ActivityExecutionContext, ValueTask<TResult>> activity, MemoryBlockReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<ValueTask<TResult>> activity, MemoryBlockReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<ActivityExecutionContext, TResult> activity, MemoryBlockReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<TResult> activity, MemoryBlockReference? output = default) => new(activity, output);
}

/// <summary>
/// Represents a composite activity that has a single <see cref="Root"/> activity and returns a result.
/// </summary>
public class Composite<T> : Activity<T>
{
    /// <summary>
    /// The activity to schedule when this activity executes.
    /// </summary>
    [Port]
    public IActivity Root { get; protected set; } = new Sequence();

    protected override void Execute(ActivityExecutionContext context)
    {
        context.ScheduleActivity(Root, OnCompletedAsync);
    }

    protected virtual ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => ValueTask.CompletedTask;

    protected static Inline From(Func<ActivityExecutionContext, ValueTask> activity) => new(activity);
    protected static Inline From(Func<ValueTask> activity) => new(activity);
    protected static Inline From(Action<ActivityExecutionContext> activity) => new(activity);
    protected static Inline From(Action activity) => new(activity);
    protected static Inline<TResult> From<TResult>(Func<ActivityExecutionContext, ValueTask<TResult>> activity, MemoryBlockReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<ValueTask<TResult>> activity, MemoryBlockReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<ActivityExecutionContext, TResult> activity, MemoryBlockReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<TResult> activity, MemoryBlockReference? output = default) => new(activity, output);
}