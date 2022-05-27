using Elsa.Mediator.Services;
using Elsa.Workflows.Management.Notifications;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.Services;

namespace Elsa.Workflows.Management.Implementations;

public class WorkflowDefinitionManager : IWorkflowDefinitionManager
{
    private readonly IWorkflowDefinitionStore _store;
    private readonly IEventPublisher _eventPublisher;

    public WorkflowDefinitionManager(IWorkflowDefinitionStore store, IEventPublisher eventPublisher)
    {
        _store = store;
        _eventPublisher = eventPublisher;
    }
    
    public async Task<int> DeleteByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var count = await _store.DeleteByDefinitionIdAsync(definitionId, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowDefinitionDeleted(definitionId), cancellationToken);
        return count;
    }

    public async Task<int> BulkDeleteByDefinitionIdsAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var ids = definitionIds.ToList();
        var count = await _store.DeleteManyByDefinitionIdsAsync(ids, cancellationToken);
        await _eventPublisher.PublishAsync(new WorkflowDefinitionsDeleted(ids), cancellationToken);
        return count;
    }
}