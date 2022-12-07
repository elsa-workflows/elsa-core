using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Behaviors;

namespace Elsa.Workflows.Core.Models;

public abstract class Activity : ActivityBase
{
    protected Activity(string? source = default, int? line = default) : base(source, line)
    {
        Behaviors.Add<AutoCompleteBehavior>(this);
    }

    protected Activity(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
        Type = activityType;
    }
}

public abstract class ActivityWithResult : Activity
{
    protected ActivityWithResult(string? source = default, int? line = default) : base(source, line)
    {
    }

    protected ActivityWithResult(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
    }

    protected ActivityWithResult(MemoryBlockReference? output, string? source = default, int? line = default) : base(source, line)
    {
        if (output != null) Result = new Output(output);
    }

    protected ActivityWithResult(Output? output, string? source = default, int? line = default) : base(source, line)
    {
        Result = output;
    }

    public Output? Result { get; set; }
}

public abstract class ActivityBaseWithResult : ActivityBase
{
    protected ActivityBaseWithResult(string? source = default, int? line = default) : base(source, line)
    {
    }

    protected ActivityBaseWithResult(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
    }

    protected ActivityBaseWithResult(MemoryBlockReference? output, string? source = default, int? line = default) : this(source, line)
    {
        if (output != null) Result = new Output(output);
    }

    protected ActivityBaseWithResult(Output? output, string? source = default, int? line = default) : this(source, line)
    {
        Result = output;
    }

    public Output? Result { get; set; }
}

public abstract class Activity<T> : Activity
{
    protected Activity(string? source = default, int? line = default) : base(source, line)
    {
    }

    protected Activity(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
    }

    protected Activity(MemoryBlockReference? output, string? source = default, int? line = default) : this(source, line)
    {
        if (output != null) Result = new Output<T>(output);
    }

    protected Activity(Output<T>? output, string? source = default, int? line = default) : this(source, line)
    {
        Result = output;
    }

    public Output<T>? Result { get; set; }
}

public abstract class ActivityBase<T> : ActivityBase
{
    protected ActivityBase(string? source = default, int? line = default) : base(source, line)
    {
    }

    protected ActivityBase(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
    }

    protected ActivityBase(MemoryBlockReference? output, string? source = default, int? line = default) : this(source, line)
    {
        if (output != null) Result = new Output<T>(output);
    }

    protected ActivityBase(Output<T>? output, string? source = default, int? line = default) : this(source, line)
    {
        Result = output;
    }

    public Output<T>? Result { get; set; }
}