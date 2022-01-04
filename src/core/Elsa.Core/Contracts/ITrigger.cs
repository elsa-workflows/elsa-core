using Elsa.Models;

namespace Elsa.Contracts;

public interface ITrigger : INode
{
    string TriggerType { get; set; }
    ValueTask<IEnumerable<object>> GetHashInputsAsync(TriggerIndexingContext context, CancellationToken cancellationToken = default);
}