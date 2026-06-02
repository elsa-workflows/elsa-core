using Elsa.Common.Models;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Persistence.ModularPersistence.Storage;

namespace Elsa.Workflows.Management.Persistence.ModularPersistence.Stores;

public interface IWorkflowInstanceMetadataStore
{
    ValueTask SaveAsync(WorkflowInstanceMetadataRecord record, CancellationToken cancellationToken = default);

    ValueTask SaveManyAsync(IEnumerable<WorkflowInstanceMetadataRecord> records, CancellationToken cancellationToken = default);

    ValueTask<WorkflowInstanceMetadataRecord?> FindAsync(string id, string? tenantId = null, CancellationToken cancellationToken = default);

    ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, string? tenantId = null, CancellationToken cancellationToken = default);

    ValueTask<Page<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, string? tenantId = null, CancellationToken cancellationToken = default);

    ValueTask<long> CountAsync(WorkflowInstanceFilter filter, string? tenantId = null, CancellationToken cancellationToken = default);
}
