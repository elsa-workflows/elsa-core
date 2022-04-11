using System.Linq.Expressions;
using Elsa.Contracts;
using Elsa.Helpers;

namespace Elsa.Models;

public abstract class Activity : IActivity
{
    protected Activity() => TypeName = TypeNameHelper.GenerateTypeName(GetType());
    protected Activity(string activityType) => TypeName = activityType;

    public string Id { get; set; } = default!;
    public string TypeName { get; set; }
    public bool CanStartWorkflow { get; set; }
    public IDictionary<string, object> ApplicationProperties { get; set; } = new Dictionary<string, object>();
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    protected virtual ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        Execute(context);
        return ValueTask.CompletedTask;
    }

    protected virtual void Execute(ActivityExecutionContext context)
    {
    }

    ValueTask IActivity.ExecuteAsync(ActivityExecutionContext context) => ExecuteAsync(context);
}

public abstract class ActivityWithResult : Activity
{
    protected ActivityWithResult()
    {
    }

    protected ActivityWithResult(string activityType) : base(activityType)
    {
    }

    protected ActivityWithResult(RegisterLocationReference? outputTarget)
    {
        if (outputTarget != null) this.CaptureOutput(outputTarget);
    }

    public Output Result { get; } = new();
}

public abstract class Activity<T> : ActivityWithResult
{
    protected Activity()
    {
    }

    protected Activity(string activityType) : base(activityType)
    {
    }

    protected Activity(RegisterLocationReference? outputTarget) : base(outputTarget)
    {
    }
}

public static class ActivityWithResultExtensions
{
    public static T CaptureOutput<T>(this T activity, Expression<Func<T, Output>> propertyExpression, RegisterLocationReference locationReference) where T:IActivity
    {
        var output = activity.GetPropertyValue(propertyExpression)!;
        output.Targets.Add(locationReference);
        return activity;
    }

    public static T CaptureOutput<T>(this T activity, RegisterLocationReference locationReference) where T : ActivityWithResult => activity.CaptureOutput(x => x.Result, locationReference);
}