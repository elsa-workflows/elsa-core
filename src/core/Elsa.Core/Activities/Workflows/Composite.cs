using Elsa.Activities.Primitives;
using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.Workflows;

/// <summary>
/// Represents a composite activity that has a single <see cref="Root"/> activity.
/// </summary>
public class Composite : Activity
{
    /// <summary>
    /// The activity to schedule when this activity executes.
    /// </summary>
    [Outbound]
    public IActivity Root { get; protected set; } = new Sequence();

    protected override void Execute(ActivityExecutionContext context)
    {
        context.SubmitActivity(Root, OnCompletedAsync);
    }

    protected virtual ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => ValueTask.CompletedTask;

    protected static Inline From(Func<ActivityExecutionContext, ValueTask> activity) => new(activity);
    protected static Inline From(Func<ValueTask> activity) => new(activity);
    protected static Inline From(Action<ActivityExecutionContext> activity) => new(activity);
    protected static Inline From(Action activity) => new(activity);
    protected static Inline<TResult> From<TResult>(Func<ActivityExecutionContext, ValueTask<TResult>> activity, RegisterLocationReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<ValueTask<TResult>> activity, RegisterLocationReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<ActivityExecutionContext, TResult> activity, RegisterLocationReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<TResult> activity, RegisterLocationReference? output = default) => new(activity, output);
}

/// <summary>
/// Represents a composite activity that has a single <see cref="Root"/> activity and returns a result.
/// </summary>
public class Composite<T> : Activity<T>
{
    /// <summary>
    /// The activity to schedule when this activity executes.
    /// </summary>
    [Outbound]
    public IActivity Root { get; protected set; } = new Sequence();

    protected override void Execute(ActivityExecutionContext context)
    {
        context.SubmitActivity(Root, OnCompletedAsync);
    }

    protected virtual ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext) => ValueTask.CompletedTask;

    protected static Inline From(Func<ActivityExecutionContext, ValueTask> activity) => new(activity);
    protected static Inline From(Func<ValueTask> activity) => new(activity);
    protected static Inline From(Action<ActivityExecutionContext> activity) => new(activity);
    protected static Inline From(Action activity) => new(activity);
    protected static Inline<TResult> From<TResult>(Func<ActivityExecutionContext, ValueTask<TResult>> activity, RegisterLocationReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<ValueTask<TResult>> activity, RegisterLocationReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<ActivityExecutionContext, TResult> activity, RegisterLocationReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<TResult> activity, RegisterLocationReference? output = default) => new(activity, output);
}