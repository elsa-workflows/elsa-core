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

    public ValueTask<IEnumerable<object>> GetBookmarkPayloadsAsync(TriggerIndexingContext context, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
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

    public Output? Result { get; set; }
}

public abstract class Activity<T> : ActivityWithResult
{
    protected Activity() : base()
    {
    }

    protected Activity(string activityType) : base(activityType)
    {
    }

    public new Output<T?>? Result { get; set; }
}