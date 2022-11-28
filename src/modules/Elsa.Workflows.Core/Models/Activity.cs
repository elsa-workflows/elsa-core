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

public abstract class Activity<T> : Activity
{
    protected Activity()
    {
    }

    protected Activity(string activityType) : base(activityType)
    {
    }

    protected Activity(MemoryBlockReference? output)
    {
        if (output != null) Result = new Output<T>(output);
    }

    protected Activity(Output<T>? output)
    {
        Result = output;
    }
    
    public Output<T>? Result { get; set; }
}

public abstract class ActivityBase<T> : ActivityBase
{
    protected ActivityBase()
    {
    }

    protected ActivityBase(string activityType) : base(activityType)
    {
    }

    protected ActivityBase(MemoryBlockReference? output)
    {
        if (output != null) Result = new Output<T>(output);
    }

    protected ActivityBase(Output<T>? output)
    {
        Result = output;
    }
    
    public Output<T>? Result { get; set; }
}