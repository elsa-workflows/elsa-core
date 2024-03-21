using Elsa.Workflows.Management.Requests;

namespace Elsa.Workflows.Management.Handlers;

/// <summary>
/// Dispatches workflow definition related notifications to refresh the activity registry.
/// </summary>
public interface IDistributedEventsDispatcher
{
    /// <summary>
    /// Dispatches a request to refresh the activity registry for the workflow definition provider.
    /// </summary>
    Task DispatchAsync(RefreshWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default);
}