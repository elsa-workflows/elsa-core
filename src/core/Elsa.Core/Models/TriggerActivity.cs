using Elsa.Contracts;

namespace Elsa.Models;

public class TriggerActivity : Activity, ITrigger
{
    protected TriggerActivity()
    {
    }

    protected TriggerActivity(string triggerType) : base(triggerType)
    {
    }

    public string TriggerType
    {
        get => NodeType;
        set => NodeType = value;
    }

    ValueTask<IEnumerable<object>> ITrigger.GetPayloadsAsync(TriggerIndexingContext context, CancellationToken cancellationToken) => GetPayloadsAsync(context, cancellationToken);
    
    /// <summary>
    /// Override this method to return a list of trigger payloads.  
    /// </summary>
    protected virtual ValueTask<IEnumerable<object>> GetPayloadsAsync(TriggerIndexingContext context, CancellationToken cancellationToken = default)
    {
        var hashes = GetPayloads(context);
        return ValueTask.FromResult(hashes);
    }

    /// <summary>
    /// Override this method to return a list of trigger payloads.
    /// </summary>
    protected virtual IEnumerable<object> GetPayloads(TriggerIndexingContext context) => new[]{ GetPayload(context) };
    
    /// <summary>
    /// Override this method to return a trigger payloads.
    /// </summary>
    protected virtual object GetPayload(TriggerIndexingContext context) => new();
}