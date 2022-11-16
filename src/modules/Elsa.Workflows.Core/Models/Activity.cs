using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Behaviors;

namespace Elsa.Workflows.Core.Models;

public abstract class Activity : ActivityBase
{
    protected Activity()
    {
        Behaviors.Add<AutoCompleteBehavior>(this);
    }

    protected Activity(string activityType) : this()
    {
        Type = activityType;
    }
}

public abstract class ActivityWithResult : Activity
{
    protected ActivityWithResult()
    {
    }

    protected ActivityWithResult(string activityType) : base(activityType)
    {
    }

    protected ActivityWithResult(MemoryBlockReference? output)
    {
        if (output != null) Result = new Output(output);
    }

    protected ActivityWithResult(Output? output)
    {
        Result = output;
    }

    public Output? Result { get; set; }
}

public abstract class ActivityBaseWithResult : ActivityBase
{
    protected ActivityBaseWithResult()
    {
    }

    protected ActivityBaseWithResult(string activityType) : base(activityType)
    {
    }

    protected ActivityBaseWithResult(MemoryBlockReference? output)
    {
        if (output != null) Result = new Output(output);
    }

    protected ActivityBaseWithResult(Output? output)
    {
        Result = output;
    }

    public Output? Result { get; set; }
}

public abstract class Activity<T> : ActivityWithResult
{
    protected Activity()
    {
    }

    protected Activity(string activityType) : base(activityType)
    {
    }

    protected Activity(MemoryBlockReference? output) : base(output)
    {
    }

    protected Activity(Output<T>? output) : base(output)
    {
    }
}

public abstract class ActivityBase<T> : ActivityBaseWithResult
{
    protected ActivityBase()
    {
    }

    protected ActivityBase(string activityType) : base(activityType)
    {
    }

    protected ActivityBase(MemoryBlockReference? output) : base(output)
    {
    }

    protected ActivityBase(Output<T>? output) : base(output)
    {
    }
}