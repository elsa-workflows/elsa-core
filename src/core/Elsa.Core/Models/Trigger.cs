using Elsa.Contracts;
using Elsa.Helpers;

namespace Elsa.Models;

public class Trigger : ITrigger
{
    protected Trigger() => NodeType = TypeNameHelper.GenerateTypeName(GetType());
    protected Trigger(string triggerType) => NodeType = triggerType;

    public string Id { get; set; } = default!;
    public string NodeType { get; set; }
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    public virtual ValueTask<IEnumerable<object>> GetPayloadsAsync(TriggerIndexingContext context, CancellationToken cancellationToken = default)
    {
        var hashes = GetPayloads(context);
        return ValueTask.FromResult(hashes);
    }

    protected virtual IEnumerable<object> GetPayloads(TriggerIndexingContext context) => Enumerable.Empty<object>();
}