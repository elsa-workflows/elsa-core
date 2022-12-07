using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Models;

public abstract class Trigger : ActivityBase, ITrigger
{
    protected Trigger(string? source = default, int? line = default) : base(source, line)
    {
    }

    protected Trigger(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
    }

    ValueTask<IEnumerable<object>> ITrigger.GetTriggerPayloadsAsync(TriggerIndexingContext context) => GetTriggerPayloadsAsync(context);

    /// <summary>
    /// Override this method to return trigger data.  
    /// </summary>
    protected virtual ValueTask<IEnumerable<object>> GetTriggerPayloadsAsync(TriggerIndexingContext context)
    {
        var hashes = GetTriggerPayloads(context);
        return ValueTask.FromResult(hashes);
    }

    /// <summary>
    /// Override this method to return trigger data.
    /// </summary>
    protected virtual IEnumerable<object> GetTriggerPayloads(TriggerIndexingContext context) => new[] { GetTriggerPayload(context) };

    /// <summary>
    /// Override this method to return a trigger datum.
    /// </summary>
    protected virtual object GetTriggerPayload(TriggerIndexingContext context) => new();
}

public abstract class Trigger<TResult> : ActivityBase<TResult>, ITrigger
{
    protected Trigger(string? source = default, int? line = default) : base(source, line)
    {
    }

    protected Trigger(string activityType, int version = 1, string? source = default, int? line = default) : base(activityType, version, source, line)
    {
    }

    ValueTask<IEnumerable<object>> ITrigger.GetTriggerPayloadsAsync(TriggerIndexingContext context) => GetTriggerPayloadsAsync(context);

    /// <summary>
    /// Override this method to return trigger data.  
    /// </summary>
    protected virtual ValueTask<IEnumerable<object>> GetTriggerPayloadsAsync(TriggerIndexingContext context)
    {
        var hashes = GetTriggerPayloads(context);
        return ValueTask.FromResult(hashes);
    }

    /// <summary>
    /// Override this method to return a trigger payload.
    /// </summary>
    protected virtual IEnumerable<object> GetTriggerPayloads(TriggerIndexingContext context) => new[] { GetTriggerPayload(context) };

    /// <summary>
    /// Override this method to return a trigger payload.
    /// </summary>
    protected virtual object GetTriggerPayload(TriggerIndexingContext context) => new();
}