namespace Elsa.Workflows.Management;

/// <summary>
/// Service for querying workflow consumers (transitive closure of references).
/// </summary>
public interface IWorkflowConsumerService
{
    /// <summary>
    /// Gets all workflow definition IDs that consume the specified workflow definition (transitive closure).
    /// This method returns the complete dependency graph by recursively following workflow references.
    /// </summary>
    /// <param name="definitionId">The workflow definition ID to find consumers for.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <param name="visitedIds">Set of already visited definition IDs to prevent infinite recursion.</param>
    /// <returns>An async enumerable of workflow definition IDs that consume the specified workflow.</returns>
    IAsyncEnumerable<string> GetConsumingWorkflowDefinitionIdsAsync(
        string definitionId,
        CancellationToken cancellationToken = default,
        HashSet<string>? visitedIds = null);
}
