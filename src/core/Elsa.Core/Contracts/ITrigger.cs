using Elsa.Models;

namespace Elsa.Contracts;

public interface ITrigger : INode
{
    ValueTask<IEnumerable<object>> GetPayloadsAsync(TriggerIndexingContext context, CancellationToken cancellationToken = default);
}