using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Signals;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Represents a composite activity that has a single <see cref="Root"/> activity. Like a workflow, but without workflow-level properties.
/// </summary>
[PublicAPI]
public abstract class Composite : Activity, IVariableContainer
{
    /// <inheritdoc />
    protected Composite(string? source = default, int? line = default) : base(source, line)
    {
        OnSignalReceived<CompleteCompositeSignal>(OnCompleteCompositeSignal);
    }

    /// <inheritdoc />
    [JsonIgnore]  // Composite activities' Variables is intended to be constructed from code only.
    public ICollection<Variable> Variables { get; init; } = new List<Variable>();

    /// <summary>
    /// A variable to allow activities to set a result.
    /// </summary>
    [JsonIgnore]
    public Variable? ResultVariable { get; set; }
    
    /// <summary>
    /// The activity to schedule when this activity executes.
    /// </summary>
    [Port]
    [Browsable(false)]
    [JsonIgnoreCompositeRoot] // Composite activities' Root is intended to be constructed from code only.
    public IActivity Root { get; set; } = new Sequence();

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        ConfigureActivities(context);
        
        // Register variables.
        foreach (var variable in Variables) 
            variable.Set(context, variable.Value);

        await context.ScheduleActivityAsync(Root, OnRootCompletedAsync);
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
    
    /// <summary>
    /// Completes this composite activity.
    /// </summary>
    protected async Task CompleteAsync(ActivityExecutionContext context, object? result = default) => await context.SendSignalAsync(new CompleteCompositeSignal(result));
    
    /// <summary>
    /// Completes this composite activity.
    /// </summary>
    protected async Task CompleteAsync(ActivityExecutionContext context, params string[] outcomes) => await CompleteAsync(context, new Outcomes(outcomes));

    protected virtual ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        OnCompleted(context, childContext);
        return new();
    }

    protected virtual void OnCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
    }

    private async ValueTask OnCompleteCompositeSignal(CompleteCompositeSignal signal, SignalContext context)
    {
        // Set the outcome into the context for the parent activity to pick up.
        context.SenderActivityExecutionContext.WorkflowExecutionContext.TransientProperties[nameof(CompleteCompositeSignal)] = signal;
        
        await OnCompletedAsync(context.ReceiverActivityExecutionContext, context.SenderActivityExecutionContext);
        
        // Complete the sender first so that it notifies its parents to complete.
        await context.SenderActivityExecutionContext.CompleteActivityAsync();
        
        // Then complete this activity.
        await context.ReceiverActivityExecutionContext.CompleteActivityAsync(signal.Value);
        context.StopPropagation();
        
    }

    /// <summary>
    /// Creates a new <see cref="Activities.Inline"/> activity.
    /// </summary>
    protected static Inline Inline(Func<ActivityExecutionContext, ValueTask> activity, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(activity, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Activities.Inline"/> activity.
    /// </summary>
    protected static Inline Inline(Func<ValueTask> activity, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(activity, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Activities.Inline"/> activity.
    /// </summary>
    protected static Inline Inline(Action<ActivityExecutionContext> activity, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(activity, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Activities.Inline"/> activity.
    /// </summary>
    protected static Inline Inline(Action activity, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(activity, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Activities.Inline"/> activity.
    /// </summary>
    protected static Inline<TResult> Inline<TResult>(Func<ActivityExecutionContext, ValueTask<TResult>> activity, MemoryBlockReference? output = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(activity, output, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Activities.Inline"/> activity.
    /// </summary>
    protected static Inline<TResult> Inline<TResult>(Func<ValueTask<TResult>> activity, MemoryBlockReference? output = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(activity, output, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Activities.Inline"/> activity.
    /// </summary>
    protected static Inline<TResult> Inline<TResult>(Func<ActivityExecutionContext, TResult> activity, MemoryBlockReference? output, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(activity, output, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Activities.Inline"/> activity.
    /// </summary>
    protected static Inline<TResult> Inline<TResult>(Func<TResult> activity, MemoryBlockReference? output = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(activity, output, source, line);

    /// <summary>
    /// Creates a new <see cref="Activities.SetVariable"/> activity.
    /// </summary>
    protected static SetVariable<T> SetVariable<T>(Variable<T> variable, T value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(variable, value, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Activities.SetVariable"/> activity.
    /// </summary>
    protected static SetVariable<T> SetVariable<T>(Variable<T> variable, Func<ExpressionExecutionContext, T> value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(variable, value, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Activities.SetVariable"/> activity.
    /// </summary>
    protected static SetVariable<T> SetVariable<T>(Variable<T> variable, Func<T> value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(variable, value, source, line);
    
    /// <summary>
    /// Creates a new <see cref="Activities.SetVariable"/> activity.
    /// </summary>
    protected static SetVariable<T> SetVariable<T>(Variable<T> variable, Variable<T> value, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) => new(variable, value, source, line);
}

/// <summary>
/// Base class for custom activities with auto-complete behavior that return a result.
/// </summary>
[PublicAPI]
public abstract class CompositeWithResult : Composite
{
    /// <inheritdoc />
    protected CompositeWithResult(string? source = default, int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected CompositeWithResult(MemoryBlockReference? output, string? source = default, int? line = default) : base(source, line)
    {
        if (output != null) Result = new Output(output);
    }

    /// <inheritdoc />
    protected CompositeWithResult(Output? output, string? source = default, int? line = default) : base(source, line)
    {
        Result = output;
    }

    /// <summary>
    /// The result value of the composite.
    /// </summary>
    [Output] public Output? Result { get; set; }
}

/// <summary>
/// Represents a composite activity that has a single <see cref="Composite.Root"/> activity and returns a result.
/// </summary>
[PublicAPI]
public abstract class Composite<T> : Composite, IActivityWithResult<T>
{
    /// <inheritdoc />
    protected Composite([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <summary>
    /// The result of the activity.
    /// </summary>
    [Output] public Output<T>? Result { get; set; }

    Output? IActivityWithResult.Result
    {
        get => Result;
        set => Result = (Output<T>?)value;
    }
}