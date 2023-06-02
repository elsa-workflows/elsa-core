using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.Workflows.Runtime.HostedServices;

/// <summary>
/// Updates the workflow store from <see cref="IWorkflowProvider"/> implementations, creates triggers and updates the <see cref="IActivityRegistry"/>.
/// </summary>
public class PopulateWorkflowDefinitionStoreAndActivityRegistry : IHostedService
{
    private readonly IWorkflowDefinitionStorePopulator _workflowDefinitionStorePopulator;
    private readonly IActivityRegistryPopulator _activityRegistryPopulator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PopulateWorkflowDefinitionStoreAndActivityRegistry"/> class.
    /// </summary>
    public PopulateWorkflowDefinitionStoreAndActivityRegistry(
        IWorkflowDefinitionStorePopulator workflowDefinitionStorePopulator,
        IActivityRegistryPopulator activityRegistryPopulator)
    {
        _workflowDefinitionStorePopulator = workflowDefinitionStorePopulator;
        _activityRegistryPopulator = activityRegistryPopulator;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Stage 1: Populate the activity registry.
        await _activityRegistryPopulator.PopulateRegistryAsync(cancellationToken);
        
        // Stage 2: Populate the workflow definition store.
        await _workflowDefinitionStorePopulator.PopulateStoreAsync(cancellationToken);
        
        // Stage 2: Re-populate the activity registry.
        await _activityRegistryPopulator.PopulateRegistryAsync(cancellationToken);
        
        // tage 3. Re-update the workflow definition store with the current set of activities.
        await _workflowDefinitionStorePopulator.PopulateStoreAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}