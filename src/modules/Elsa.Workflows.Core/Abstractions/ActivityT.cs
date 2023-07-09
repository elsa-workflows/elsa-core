using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core;

/// <summary>
/// Base class for custom activities that return a result.
/// </summary>
public abstract class Activity<T> : Activity, IActivityWithResult<T>
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

    Output? IActivityWithResult.Result
    {
        get => Result;
        set => Result = (Output<T>?)value;
    }
}