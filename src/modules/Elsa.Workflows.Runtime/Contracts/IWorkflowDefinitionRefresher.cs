using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime.Contracts;

/// Refreshes all workflows by re-indexing their triggers.
public interface IWorkflowDefinitionsRefresher
{
    /// Refreshes all workflows by re-indexing their triggers.
    Task<RefreshWorkflowDefinitionsResponse> RefreshWorkflowDefinitionsAsync(RefreshWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default);
}