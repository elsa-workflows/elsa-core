using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Providers;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class DefaultRegistriesPopulator(IWorkflowDefinitionStorePopulator workflowDefinitionStorePopulator, IActivityRegistryPopulator activityRegistryPopulator, IActivityRegistry registry, TypedActivityProvider typedActivityProvider) : IRegistriesPopulator
{
    /// <inheritdoc />
    public async Task PopulateAsync(CancellationToken cancellationToken = default)
    {
        // Stage 1: Populate the typed activity registry.
        // This is necessary to ensure that the activity descriptors are up-to-date before we populate the workflow definition store.
        await registry.RefreshDescriptorsAsync(typedActivityProvider, cancellationToken);

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