using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Behaviors;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Core;

/// <summary>
/// Base class for custom activities with auto-complete behavior.
/// </summary>
[PublicAPI]
public abstract class CodeActivity : Activity
{
    /// <inheritdoc />
    protected CodeActivity(string? source = default, int? line = default) : base(source, line)
    {
        Behaviors.Add<AutoCompleteBehavior>(this);
    }

    /// <inheritdoc />
    protected CodeActivity(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
        Type = activityType;
    }
}

/// <summary>
/// Base class for custom activities with auto-complete behavior that return a result.
/// </summary>
[PublicAPI]
public abstract class CodeActivityWithResult : CodeActivity
{
    /// <inheritdoc />
    protected CodeActivityWithResult(string? source = default, int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected CodeActivityWithResult(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
    }

    /// <inheritdoc />
    protected CodeActivityWithResult(MemoryBlockReference? output, string? source = default, int? line = default) : base(source, line)
    {
        if (output != null) Result = new Output(output);
    }

    /// <inheritdoc />
    protected CodeActivityWithResult(Output? output, string? source = default, int? line = default) : base(source, line)
    {
        Result = output;
    }

    /// <summary>
    /// The result of the activity.
    /// </summary>
    public Output? Result { get; set; }
}

/// <summary>
/// Base class for custom activities with auto-complete behavior that return a result.
/// </summary>
public abstract class CodeActivity<T> : CodeActivity, IActivityWithResult<T>
{
    /// <inheritdoc />
    protected CodeActivity(string? source = default, int? line = default) : base(source, line)
    {
    }
    
    /// <inheritdoc />
    protected CodeActivity(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
    }

    /// <inheritdoc />
    protected CodeActivity(MemoryBlockReference? output, string? source = default, int? line = default) : this(source, line)
    {
        if (output != null) Result = new Output<T>(output);
    }

    /// <inheritdoc />
    protected CodeActivity(Output<T>? output, string? source = default, int? line = default) : this(source, line)
    {
        Result = output;
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