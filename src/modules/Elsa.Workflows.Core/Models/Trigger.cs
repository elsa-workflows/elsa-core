using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

public abstract class Trigger : Activity, ITrigger
{
    protected Trigger()
    {
    }

    protected Trigger(string activityType) : base(activityType)
    {
    }

    ValueTask<IEnumerable<object>> ITrigger.GetTriggerDataAsync(TriggerIndexingContext context) => GetTriggerDataAsync(context);

    /// <summary>
    /// Override this method to return trigger data.  
    /// </summary>
    protected virtual ValueTask<IEnumerable<object>> GetTriggerDataAsync(TriggerIndexingContext context)
    {
        var hashes = GetTriggerData(context);
        return ValueTask.FromResult(hashes);
    }

    /// <summary>
    /// Override this method to return trigger data.
    /// </summary>
    protected virtual IEnumerable<object> GetTriggerData(TriggerIndexingContext context) => new[] { GetTriggerPayload(context) };

    /// <summary>
    /// Override this method to return a trigger datum.
    /// </summary>
    protected virtual object GetTriggerPayload(TriggerIndexingContext context) => new();
}

public abstract class Trigger<T> : Activity<T>, ITrigger
{
    protected Trigger()
    {
    }

    protected Trigger(string activityType) : base(activityType)
    {
    }

    // protected Trigger(MemoryReference? outputTarget) : base(outputTarget)
    // {
    // }

    ValueTask<IEnumerable<object>> ITrigger.GetTriggerDataAsync(TriggerIndexingContext context) => GetTriggerDataAsync(context);

    /// <summary>
    /// Override this method to return trigger data.  
    /// </summary>
    protected virtual ValueTask<IEnumerable<object>> GetTriggerDataAsync(TriggerIndexingContext context)
    {
        var hashes = GetTriggerPayload(context);
        return ValueTask.FromResult(hashes);
    }

    /// <summary>
    /// Override this method to return trigger data.
    /// </summary>
    protected virtual IEnumerable<object> GetTriggerPayload(TriggerIndexingContext context) => new[] { GetTriggerDatum(context) };

    /// <summary>
    /// Override this method to return a trigger datum.
    /// </summary>
    protected virtual object GetTriggerDatum(TriggerIndexingContext context) => new();
}