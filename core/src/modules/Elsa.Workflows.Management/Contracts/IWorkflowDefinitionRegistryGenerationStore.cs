namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Stores durable workflow-definition registry generations used to reconcile registries across nodes.
/// </summary>
public interface IWorkflowDefinitionRegistryGenerationStore
{
    /// <summary>
    /// Gets whether this store is shared by every application node.
    /// </summary>
    bool IsShared { get; }

    /// <summary>
    /// Advances the registry generation for the specified tenant.
    /// </summary>
    Task<long> IncrementAsync(string? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current registry generation for the specified tenant.
    /// </summary>
    Task<long> GetGenerationAsync(string? tenantId, CancellationToken cancellationToken = default);

}
