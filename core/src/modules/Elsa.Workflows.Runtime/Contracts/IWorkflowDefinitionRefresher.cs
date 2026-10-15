using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Refreshes all workflows by re-indexing their triggers.
/// </summary>
public interface IWorkflowDefinitionsRefresher
{
    /// <summary>
    /// Refreshes all workflows by re-indexing their triggers.
    /// </summary>
    Task<RefreshWorkflowDefinitionsResponse> RefreshWorkflowDefinitionsAsync(RefreshWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default);
}