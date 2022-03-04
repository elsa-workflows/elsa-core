using Elsa.Contracts;

namespace Elsa.Models;

public abstract class Trigger : Activity, ITrigger
{
    protected Trigger()
    {
    }

    protected Trigger(string triggerType) : base(triggerType)
    {
    }

    public TriggerMode TriggerMode { get; set; } = TriggerMode.WorkflowDefinition;
    ValueTask<IEnumerable<object>> ITrigger.GetTriggerPayloadsAsync(TriggerIndexingContext context, CancellationToken cancellationToken) => GetTriggerPayloadsAsync(context, cancellationToken);
    
    /// <summary>
    /// Override this method to return a list of trigger payloads.  
    /// </summary>
    protected virtual ValueTask<IEnumerable<object>> GetTriggerPayloadsAsync(TriggerIndexingContext context, CancellationToken cancellationToken = default)
    {
        var hashes = GetTriggerPayloads(context);
        return ValueTask.FromResult(hashes);
    }

    /// <summary>
    /// Override this method to return a list of trigger payloads.
    /// </summary>
    protected virtual IEnumerable<object> GetTriggerPayloads(TriggerIndexingContext context) => new[]{ GetTriggerPayload(context) };
    
    /// <summary>
    /// Override this method to return a trigger payload.
    /// </summary>
    protected virtual object GetTriggerPayload(TriggerIndexingContext context) => new();
}