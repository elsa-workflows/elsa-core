using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Behaviors;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// Base class for custom activities with auto-complete behavior.
/// </summary>
public abstract class Activity : ActivityBase
{
    /// <inheritdoc />
    protected Activity(string? source = default, int? line = default) : base(source, line)
    {
        Behaviors.Add<AutoCompleteBehavior>(this);
    }

    /// <inheritdoc />
    protected Activity(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
        Type = activityType;
    }
}

/// <summary>
/// Base class for custom activities with auto-complete behavior that return a result.
/// </summary>
public abstract class ActivityWithResult : Activity
{
    /// <inheritdoc />
    protected ActivityWithResult(string? source = default, int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected ActivityWithResult(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
    }

    /// <inheritdoc />
    protected ActivityWithResult(MemoryBlockReference? output, string? source = default, int? line = default) : base(source, line)
    {
        if (output != null) Result = new Output(output);
    }

    /// <inheritdoc />
    protected ActivityWithResult(Output? output, string? source = default, int? line = default) : base(source, line)
    {
        Result = output;
    }

    /// <summary>
    /// The result of the activity.
    /// </summary>
    public Output? Result { get; set; }
}

/// <summary>
/// Base class for custom activities that return a result.
/// </summary>
public abstract class ActivityBaseWithResult : ActivityBase
{
    /// <inheritdoc />
    protected ActivityBaseWithResult(string? source = default, int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected ActivityBaseWithResult(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
    }

    /// <inheritdoc />
    protected ActivityBaseWithResult(MemoryBlockReference? output, string? source = default, int? line = default) : this(source, line)
    {
        if (output != null) Result = new Output(output);
    }

    /// <inheritdoc />
    protected ActivityBaseWithResult(Output? output, string? source = default, int? line = default) : this(source, line)
    {
        Result = output;
    }

    public Output? Result { get; set; }
}

/// <summary>
/// Base class for custom activities with auto-complete behavior that return a result.
/// </summary>
public abstract class Activity<T> : Activity
{
    /// <inheritdoc />
    protected Activity(string? source = default, int? line = default) : base(source, line)
    {
    }
    
    /// <inheritdoc />
    protected Activity(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
    }

    /// <inheritdoc />
    protected Activity(MemoryBlockReference? output, string? source = default, int? line = default) : this(source, line)
    {
        if (output != null) Result = new Output<T>(output);
    }

    /// <inheritdoc />
    protected Activity(Output<T>? output, string? source = default, int? line = default) : this(source, line)
    {
        Result = output;
    }

    /// <summary>
    /// The result of the activity.
    /// </summary>
    public Output<T>? Result { get; set; }
}

/// <summary>
/// Base class for custom activities that return a result.
/// </summary>
public abstract class ActivityBase<T> : ActivityBase
{
    /// <inheritdoc />
    protected ActivityBase(string? source = default, int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    protected ActivityBase(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
    }

    /// <inheritdoc />
    protected ActivityBase(MemoryBlockReference? output, string? source = default, int? line = default) : this(source, line)
    {
        if (output != null) Result = new Output<T>(output);
    }

    /// <inheritdoc />
    protected ActivityBase(Output<T>? output, string? source = default, int? line = default) : this(source, line)
    {
        Result = output;
    }

    /// <summary>
    /// The result of the activity.
    /// </summary>
    public Output<T>? Result { get; set; }
}