using Elsa.Workflows.Management.Handlers;
using Elsa.Workflows.Management.Requests;
using MassTransit;

namespace Elsa.Workflows.Management.MassTransit.Services;

/// <summary>
/// Dispatches workflow definition related notifications via MassTransit.
/// </summary>
public class MassTransitWorkflowDefinitionDispatcher(IBus bus) : IWorkflowDefinitionDispatcher
{
    /// <inheritdoc />
    public async Task DispatchAsync(RefreshWorkflowDefinitionsRequest request, CancellationToken cancellationToken = default)
    {
        await bus.Publish(request, cancellationToken);
    }
}