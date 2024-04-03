using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Http.Contracts;

public interface IHttpWorkflowsCacheManager
{
    Task<(Workflow? Workflow, ICollection<StoredTrigger> Triggers)?> FindCachedWorkflowAsync(string bookmarkHash, CancellationToken cancellationToken = default);
    void EvictCachedWorkflow(string workflowDefinitionId);
}