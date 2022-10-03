using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Services;

public interface ITriggerStore
{
    Task SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default);
    Task<IEnumerable<StoredTrigger>> FindAsync(string name, string? hash, CancellationToken cancellationToken = default);
    Task<IEnumerable<StoredTrigger>> FindManyByWorkflowDefinitionIdAsync(string workflowDefinitionId, CancellationToken cancellationToken = default);
    Task ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default);
    Task DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
}