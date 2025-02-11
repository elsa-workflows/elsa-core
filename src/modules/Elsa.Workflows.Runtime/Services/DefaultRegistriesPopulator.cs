using Elsa.Workflows.Management;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class DefaultRegistriesPopulator(IWorkflowDefinitionStorePopulator workflowDefinitionStorePopulator, IActivityRegistryPopulator activityRegistryPopulator) : IRegistriesPopulator
{
    /// <inheritdoc />
    public async Task PopulateAsync(CancellationToken cancellationToken = default)
    {
        // Stage 1: Populate the activity registry.
        // Because workflow definitions can be used as activities, we need to make sure that the activity registry is populated before we populate the workflow definition store.
        await activityRegistryPopulator.PopulateRegistryAsync(cancellationToken);
        
        // Stage 2: Populate the workflow definition store.
        await workflowDefinitionStorePopulator.PopulateStoreAsync(false, cancellationToken);

        // Stage 3: Re-populate the activity registry.
        // After the workflow definition store has been populated, we need to re-populate the activity registry to make sure that the activity descriptors are up-to-date.
        await activityRegistryPopulator.PopulateRegistryAsync(cancellationToken);

        // Stage 4. Re-update the workflow definition store with the current set of activities.
        // Finally, we need to re-populate the workflow definition store to make sure that the workflow definitions are up-to-date.
        await workflowDefinitionStorePopulator.PopulateStoreAsync(true, cancellationToken);
    }
}