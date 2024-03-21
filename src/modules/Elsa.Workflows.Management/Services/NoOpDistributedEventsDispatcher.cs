using Elsa.Workflows.Management.Handlers;
using Elsa.Workflows.Management.Requests;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Dispatcher that does not dispatch any messages.
/// </summary>
public class NoOpDistributedEventsDispatcher : IDistributedEventsDispatcher
{
    /// <inheritdoc />
    public Task DispatchAsync(RefreshWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}