using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.Workflows.Runtime.HostedServices;

/// <summary>
/// Updates the workflow store from <see cref="IWorkflowProvider"/> implementations and creates triggers.
/// </summary>
public class PopulateWorkflowDefinitionStore : IHostedService
{
    private readonly IWorkflowDefinitionStorePopulator _workflowDefinitionStorePopulator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PopulateWorkflowDefinitionStore"/> class.
    /// </summary>
    public PopulateWorkflowDefinitionStore(IWorkflowDefinitionStorePopulator workflowDefinitionStorePopulator)
    {
        _workflowDefinitionStorePopulator = workflowDefinitionStorePopulator;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _workflowDefinitionStorePopulator.PopulateStoreAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}