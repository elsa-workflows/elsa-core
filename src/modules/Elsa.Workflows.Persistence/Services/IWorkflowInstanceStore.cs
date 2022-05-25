using Elsa.Persistence.Common.Entities;
using Elsa.Persistence.Common.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Models;

namespace Elsa.Workflows.Persistence.Services;

public interface IWorkflowInstanceStore
{
    Task<WorkflowInstance?> FindByIdAsync(string id, CancellationToken cancellationToken = default);
    Task SaveAsync(WorkflowInstance record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<WorkflowInstance> records, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task DeleteManyByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default);
    Task<Page<WorkflowInstanceSummary>> FindManyAsync(FindWorkflowInstancesArgs args, CancellationToken cancellationToken = default);
}

public record FindWorkflowInstancesArgs(
    string? SearchTerm = default,
    string? DefinitionId = default,
    int? Version = default,
    string? CorrelationId = default,
    WorkflowStatus? WorkflowStatus = default,
    WorkflowSubStatus? WorkflowSubStatus = default,
    PageArgs? PageArgs = default,
    OrderBy OrderBy = OrderBy.Created,
    OrderDirection OrderDirection = OrderDirection.Ascending
);