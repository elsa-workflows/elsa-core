using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// Default implementation of <see cref="IWorkflowReferenceGraphBuilder"/> that uses <see cref="IWorkflowReferenceQuery"/>
/// to recursively build a complete graph of workflow references.
/// </summary>
public class WorkflowReferenceGraphBuilder(IWorkflowReferenceQuery workflowReferenceQuery, IOptions<WorkflowReferenceGraphOptions> options) : IWorkflowReferenceGraphBuilder
{
    private readonly WorkflowReferenceGraphOptions _options = options.Value;
    /// <inheritdoc />
    public async Task<WorkflowReferenceGraph> BuildGraphAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var edges = await BuildEdgesAsync(definitionId, cancellationToken).ToListAsync(cancellationToken);
        return new WorkflowReferenceGraph([definitionId], edges);
    }

    /// <inheritdoc />
    public async Task<WorkflowReferenceGraph> BuildGraphAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var allEdges = new List<WorkflowReferenceEdge>();
        var visitedIds = new HashSet<string>();
        var rootIds = definitionIds.ToList();

        foreach (var definitionId in rootIds)
        {
            await foreach (var edge in BuildEdgesAsync(definitionId, cancellationToken, visitedIds))
            {
                allEdges.Add(edge);
            }
        }

        // Deduplicate edges
        var distinctEdges = allEdges.Distinct().ToList();

        return new WorkflowReferenceGraph(rootIds, distinctEdges);
    }

    private async IAsyncEnumerable<WorkflowReferenceEdge> BuildEdgesAsync(
        string definitionId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken,
        HashSet<string>? visitedIds = null,
        int currentDepth = 0)
    {
        visitedIds ??= new HashSet<string>();

        // Check depth limit
        if (_options.MaxDepth > 0 && currentDepth >= _options.MaxDepth)
            yield break;

        // Check max definitions limit
        if (_options.MaxDefinitions > 0 && visitedIds.Count >= _options.MaxDefinitions)
            yield break;

        // If we've already processed this definition ID, skip it to prevent infinite recursion.
        if (!visitedIds.Add(definitionId))
            yield break;

        var consumerIds = (await workflowReferenceQuery.ExecuteAsync(definitionId, cancellationToken)).ToList();
        
        // For each consumer, create an edge: Consumer (Source) → definitionId (Target)
        foreach (var consumerId in consumerIds)
        {
            yield return new WorkflowReferenceEdge(Source: consumerId, Target: definitionId);
            
            // Recursively process the consumer
            await foreach (var childEdge in BuildEdgesAsync(consumerId, cancellationToken, visitedIds, currentDepth + 1))
                yield return childEdge;
        }
    }
}

