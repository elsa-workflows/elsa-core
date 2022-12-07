using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Represents an inline code activity that can be used to execute arbitrary .NET code from a workflow.
/// </summary>
[Activity("Elsa", "Primitives", "Evaluate a Boolean condition to determine which path to execute next.")]
public class Inline : Activity
{
    private readonly Func<ActivityExecutionContext, ValueTask> _activity;

    public Inline(Func<ActivityExecutionContext, ValueTask> activity, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        _activity = activity;
    }

    public Inline(Func<ValueTask> activity, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(_ => activity(), source, line)
    {
    }

    public Inline(Action<ActivityExecutionContext> activity, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(c =>
    {
        activity(c);
        return new ValueTask();
    }, source, line)
    {
    }

    public Inline(Action activity, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(c =>
    {
        activity();
        return new ValueTask();
    }, source, line)
    {
    }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context) => await _activity(context);

    public static Inline From(Func<ActivityExecutionContext, ValueTask> activity) => new(activity);
    public static Inline From(Func<ValueTask> activity) => new(activity);
    public static Inline From(Action<ActivityExecutionContext> activity) => new(activity);
    public static Inline From(Action activity) => new(activity);

    public static Inline<T> From<T>(Func<ActivityExecutionContext, ValueTask<T>> activity) => new(activity);
    public static Inline<T> From<T>(Func<ValueTask<T>> activity) => new(activity);
    public static Inline<T> From<T>(Func<ActivityExecutionContext, T> activity) => new(activity);
    public static Inline<T> From<T>(Func<T> activity) => new(activity);
}

/// <summary>
/// Represents an inline code activity that can be used to execute arbitrary .NET code from a workflow and return a value.
/// </summary>
public class Inline<T> : Activity<T>
{
    private readonly Func<ActivityExecutionContext, ValueTask<T>> _activity;

    public Inline(Func<ActivityExecutionContext, ValueTask<T>> activity, MemoryBlockReference? output = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : base(output, source, line)
    {
        _activity = activity;
    }

    public Inline(Func<ValueTask<T>> activity, MemoryBlockReference? output = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(_ => activity(), output, source, line)
    {
    }

    public Inline(Func<ActivityExecutionContext, T> activity, MemoryBlockReference? output = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(c =>
    {
        var result = activity(c);
        return new ValueTask<T>(result);
    }, output, source, line)
    {
    }

    public Inline(Func<T> activity, MemoryBlockReference? output = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(c =>
    {
        var result = activity();
        return new ValueTask<T>(result);
    }, output, source, line)
    {
    }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var result = await _activity(context);
        context.Set(Result, result);
    }
}