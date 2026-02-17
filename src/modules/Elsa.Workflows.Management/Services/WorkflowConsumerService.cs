using System.Runtime.CompilerServices;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Service for querying workflow consumers (transitive closure of references).
/// </summary>
public class WorkflowConsumerService(IWorkflowReferenceQuery workflowReferenceQuery) : IWorkflowConsumerService
{
    /// <inheritdoc />
    public async IAsyncEnumerable<string> GetConsumingWorkflowDefinitionIdsAsync(
        string definitionId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        HashSet<string>? visitedIds = null)
    {
        visitedIds ??= new HashSet<string>();

        // If we've already processed this definition ID, skip it to prevent infinite recursion.
        if (!visitedIds.Add(definitionId))
            yield break;

        // Get direct references
        var directRefs = await workflowReferenceQuery.ExecuteAsync(definitionId, cancellationToken);

        foreach (var refId in directRefs)
        {
            yield return refId;

            // Recursively get consumers of consumers
            await foreach (var transitiveRef in GetConsumingWorkflowDefinitionIdsAsync(refId, cancellationToken, visitedIds))
                yield return transitiveRef;
        }
    }
}
