using System.ComponentModel;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.Signals;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Represents a composite activity that has a single <see cref="Root"/> activity. Like a workflow, but without workflow-level properties.
/// </summary>
[Activity("Elsa", "Workflows", "Execute a root activity that you can configure yourself")]
public class Composite : ActivityBase
{
    /// <summary>
    /// The activity to schedule when this activity executes.
    /// </summary>
    [Port]
    [Browsable(false)]
    public IActivity Root { get; set; } = new Sequence();

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        ConfigureActivities(context);
        context.ScheduleActivity(Root, OnRootCompletedAsync);
    }

    /// <summary>
    /// Override this method to configure activity properties before execution.
    /// </summary>
    protected virtual void ConfigureActivities(ActivityExecutionContext context)
    {
    }

    private async ValueTask OnRootCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await OnCompletedAsync(context, childContext);
        await context.CompleteActivityAsync();
    }

    protected virtual  ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        OnCompleted(context, childContext);
        return new();
    }

    protected virtual  void OnCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
    }

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
public class Composite<T> : ActivityBase<T>
{
    public Composite()
    {
        OnSignalReceived<CompleteCompositeSignal>(OnCompleteCompositeSignal);
    }

    /// <summary>
    /// The activity to schedule when this activity executes.
    /// </summary>
    [Port]
    [Browsable(false)]
    public IActivity Root { get; protected set; } = new Sequence();

    protected override void Execute(ActivityExecutionContext context)
    {
        ConfigureActivities(context);
        context.ScheduleActivity(Root, OnRootCompletedAsync);
    }
    
    /// <summary>
    /// Override this method to configure activity properties before execution.
    /// </summary>
    protected virtual void ConfigureActivities(ActivityExecutionContext context)
    {
    }

    private async ValueTask OnRootCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        await OnCompletedAsync(context, childContext);
        await context.CompleteActivityAsync();
    }

    protected virtual ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        OnCompleted(context, childContext);
        return new();
    }

    protected virtual void OnCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
    }

    protected static Inline From(Func<ActivityExecutionContext, ValueTask> activity) => new(activity);
    protected static Inline From(Func<ValueTask> activity) => new(activity);
    protected static Inline From(Action<ActivityExecutionContext> activity) => new(activity);
    protected static Inline From(Action activity) => new(activity);
    protected static Inline<TResult> From<TResult>(Func<ActivityExecutionContext, ValueTask<TResult>> activity, MemoryBlockReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<ValueTask<TResult>> activity, MemoryBlockReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<ActivityExecutionContext, TResult> activity, MemoryBlockReference? output = default) => new(activity, output);
    protected static Inline<TResult> From<TResult>(Func<TResult> activity, MemoryBlockReference? output = default) => new(activity, output);
    
    private async ValueTask OnCompleteCompositeSignal(CompleteCompositeSignal signal, SignalContext context)
    {
        await context.ReceiverActivityExecutionContext.CompleteActivityAsync(signal.Result);
        context.StopPropagation();
    }
}