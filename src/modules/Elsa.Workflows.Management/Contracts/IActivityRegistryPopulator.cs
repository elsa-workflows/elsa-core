using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Populates the <see cref="IActivityRegistry"/> with activities.
/// </summary>
public interface IActivityRegistryPopulator
{
    /// <summary>
    /// Populates the <see cref="IActivityRegistry"/> with activities.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PopulateRegistryAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Populates the <see cref="IActivityRegistry"/> with activities for the specified provider.
    /// </summary>
    /// <param name="providerType">The type of the provider.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PopulateRegistryAsync(Type providerType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries to add a workflow as an activity to the registry.
    /// </summary>
    /// <param name="providerType">The type of the activity provider.</param>
    /// <param name="workflowDefinitionId">The ID of the workflow definition.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task AddToRegistry(Type providerType, string workflowDefinitionId, CancellationToken cancellationToken = default);
}