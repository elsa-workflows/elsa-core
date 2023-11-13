using Elsa.Expressions.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.HostedServices;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class DefaultRegistriesPopulator : IRegistriesPopulator
{
    private readonly IWorkflowDefinitionStorePopulator _workflowDefinitionStorePopulator;
    private readonly IActivityRegistryPopulator _activityRegistryPopulator;
    private readonly IExpressionDescriptorRegistryPopulator _expressionDescriptorRegistryPopulator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PopulateRegistriesHostedService"/> class.
    /// </summary>
    public DefaultRegistriesPopulator(
        IWorkflowDefinitionStorePopulator workflowDefinitionStorePopulator,
        IActivityRegistryPopulator activityRegistryPopulator,
        IExpressionDescriptorRegistryPopulator expressionDescriptorRegistryPopulator)
    {
        _workflowDefinitionStorePopulator = workflowDefinitionStorePopulator;
        _activityRegistryPopulator = activityRegistryPopulator;
        _expressionDescriptorRegistryPopulator = expressionDescriptorRegistryPopulator;
    }

    /// <inheritdoc />
    public async Task PopulateAsync(CancellationToken cancellationToken = default)
    {
        // Stage 0: Populate the expression syntax registry.
        await _expressionDescriptorRegistryPopulator.PopulateRegistryAsync(cancellationToken);

        // Stage 1: Populate the activity registry.
        // Because workflow definitions can be used as activities, we need to make sure that the activity registry is populated before we populate the workflow definition store.
        await _activityRegistryPopulator.PopulateRegistryAsync(cancellationToken);

        // Stage 2: Populate the workflow definition store.
        await _workflowDefinitionStorePopulator.PopulateStoreAsync(cancellationToken);

        // Stage 3: Re-populate the activity registry.
        // After the workflow definition store has been populated, we need to re-populate the activity registry to make sure that the activity descriptors are up-to-date.
        await _activityRegistryPopulator.PopulateRegistryAsync(cancellationToken);

        // Stage 4. Re-update the workflow definition store with the current set of activities.
        // Finally, we need to re-populate the workflow definition store to make sure that the workflow definitions are up-to-date.
        await _workflowDefinitionStorePopulator.PopulateStoreAsync(cancellationToken);
    }
}