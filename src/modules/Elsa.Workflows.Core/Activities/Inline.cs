using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Represents an inline code activity that can be used to execute arbitrary .NET code from a workflow.
/// </summary>
[Browsable(false)]
[Activity("Elsa", "Primitives", "Evaluate a Boolean condition to determine which path to execute next.")]
public class Inline : CodeActivity
{
    private readonly Func<ActivityExecutionContext, ValueTask> _activity = default!;

    /// <inheritdoc />
    [JsonConstructor]
    public Inline([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }
    
    /// <inheritdoc />
    public Inline(Func<ActivityExecutionContext, ValueTask> activity, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
        _activity = activity;
    }

    /// <inheritdoc />
    public Inline(Func<ValueTask> activity, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(_ => activity(), source, line)
    {
    }

    /// <inheritdoc />
    public Inline(Action<ActivityExecutionContext> activity, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(c =>
    {
        activity(c);
        return new ValueTask();
    }, source, line)
    {
    }

    /// <inheritdoc />
    public Inline(Action activity, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(c =>
    {
        activity();
        return new ValueTask();
    }, source, line)
    {
    }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context) => await _activity(context);

    /// <summary>
    /// Creates a new <see cref="Inline"/> activity from the specified delegate.
    /// </summary>
    public static Inline From(Func<ActivityExecutionContext, ValueTask> activity) => new(activity);
    
    /// <summary>
    /// Creates a new <see cref="Inline"/> activity from the specified delegate.
    /// </summary>
    public static Inline From(Func<ValueTask> activity) => new(activity);
    
    /// <summary>
    /// Creates a new <see cref="Inline"/> activity from the specified delegate.
    /// </summary>
    public static Inline From(Action<ActivityExecutionContext> activity) => new(activity);
    
    /// <summary>
    /// Creates a new <see cref="Inline"/> activity from the specified delegate.
    /// </summary>
    public static Inline From(Action activity) => new(activity);

    /// <summary>
    /// Creates a new <see cref="Inline"/> activity from the specified delegate.
    /// </summary>
    public static Inline<T> From<T>(Func<ActivityExecutionContext, ValueTask<T>> activity) => new(activity);
    
    /// <summary>
    /// Creates a new <see cref="Inline"/> activity from the specified delegate.
    /// </summary>
    public static Inline<T> From<T>(Func<ValueTask<T>> activity) => new(activity);
    
    /// <summary>
    /// Creates a new <see cref="Inline"/> activity from the specified delegate.
    /// </summary>
    public static Inline<T> From<T>(Func<ActivityExecutionContext, T> activity) => new(activity);
    
    /// <summary>
    /// Creates a new <see cref="Inline"/> activity from the specified delegate.
    /// </summary>
    public static Inline<T> From<T>(Func<T> activity) => new(activity);
}

/// <summary>
/// Represents an inline code activity that can be used to execute arbitrary .NET code from a workflow and return a value.
/// </summary>
public class Inline<T> : CodeActivity<T>
{
    private readonly Func<ActivityExecutionContext, ValueTask<T>> _activity;

    /// <inheritdoc />
    public Inline(Func<ActivityExecutionContext, ValueTask<T>> activity, MemoryBlockReference? output = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : base(output, source, line)
    {
        _activity = activity;
    }

    /// <inheritdoc />
    public Inline(Func<ValueTask<T>> activity, MemoryBlockReference? output = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(_ => activity(), output, source, line)
    {
    }

    /// <inheritdoc />
    public Inline(Func<ActivityExecutionContext, T> activity, MemoryBlockReference? output = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(c =>
    {
        var result = activity(c);
        return new ValueTask<T>(result);
    }, output, source, line)
    {
    }

    /// <inheritdoc />
    public Inline(Func<T> activity, MemoryBlockReference? output = default, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) 
        : this(c =>
    {
        var result = activity();
        return new ValueTask<T>(result);
    }, output, source, line)
    {
    }
    
    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var result = await _activity(context);
        context.Set(Result, result);
    }
}