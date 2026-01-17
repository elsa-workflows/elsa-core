using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionService(
    IWorkflowDefinitionStore workflowDefinitionStore,
    IWorkflowGraphBuilder workflowGraphBuilder,
    IMaterializerRegistry materializerRegistry,
    ILogger<WorkflowDefinitionService> logger)
    : IWorkflowDefinitionService
{
    /// <inheritdoc />
    public async Task<WorkflowGraph> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var materializer = materializerRegistry.GetMaterializer(definition.MaterializerName);

        if (materializer == null)
            throw new($"Materializer '{definition.MaterializerName}' not found. The materializer may be disabled or not registered.");

        var workflow = await materializer.MaterializeAsync(definition, cancellationToken);
        return await workflowGraphBuilder.BuildAsync(workflow, cancellationToken);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var handle = WorkflowDefinitionHandle.ByDefinitionId(definitionId, versionOptions);
        return FindWorkflowDefinitionAsync(handle, cancellationToken);
    }

    /// <inheritdoc />
    public Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var handle = WorkflowDefinitionHandle.ByDefinitionVersionId(definitionVersionId);
        return FindWorkflowDefinitionAsync(handle, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(WorkflowDefinitionHandle handle, CancellationToken cancellationToken = default)
    {
        var filter = handle.ToFilter();
        return await workflowDefinitionStore.FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await workflowDefinitionStore.FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionId, versionOptions, cancellationToken);
        return await TryMaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionVersionId, cancellationToken);
        return await TryMaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(WorkflowDefinitionHandle definitionHandle, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionHandle, cancellationToken);
        return await TryMaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(filter, cancellationToken);
        return await TryMaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowGraph>> FindWorkflowGraphsAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var workflowDefinitions = await workflowDefinitionStore.FindManyAsync(filter, cancellationToken);
        var workflowGraphs = new List<WorkflowGraph>();
        foreach (var workflowDefinition in workflowDefinitions)
        {
            var workflowGraph = await MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
            workflowGraphs.Add(workflowGraph);
        }

        return workflowGraphs;
    }

    public async Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionId, versionOptions, cancellationToken);
        return await MaterializeWorkflowGraphFindResultAsync(definition, cancellationToken);
    }

    public async Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionVersionId, cancellationToken);
        return await MaterializeWorkflowGraphFindResultAsync(definition, cancellationToken);
    }

    public async Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(WorkflowDefinitionHandle definitionHandle, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionHandle, cancellationToken);
        return await MaterializeWorkflowGraphFindResultAsync(definition, cancellationToken);
    }

    public async Task<WorkflowGraphFindResult> TryFindWorkflowGraphAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(filter, cancellationToken);
        return await MaterializeWorkflowGraphFindResultAsync(definition, cancellationToken);
    }

    public async Task<IEnumerable<WorkflowGraphFindResult>> TryFindWorkflowGraphsAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var workflowDefinitions = await workflowDefinitionStore.FindManyAsync(filter, cancellationToken);
        var results = new List<WorkflowGraphFindResult>();
        foreach (var workflowDefinition in workflowDefinitions)
        {
            var result = await MaterializeWorkflowGraphFindResultAsync(workflowDefinition, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Attempts to materialize a workflow graph from the given workflow definition if a suitable materializer is available.
    /// </summary>
    /// <param name="definition">The workflow definition to materialize. Can be null.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="WorkflowGraph"/> if materialization is successful; otherwise, null.
    /// </returns>
    private async Task<WorkflowGraph?> TryMaterializeWorkflowAsync(WorkflowDefinition? definition, CancellationToken cancellationToken)
    {
        if (definition == null)
            return null;

        if (materializerRegistry.IsMaterializerAvailable(definition.MaterializerName))
            return await MaterializeWorkflowAsync(definition, cancellationToken);

        logger.LogWarning("Materializer '{MaterializerName}' not found. The workflow definition will not be materialized.", definition.MaterializerName);
        return null;
    }

    /// <summary>
    /// Attempts to materialize a workflow graph from the given workflow definition.
    /// </summary>
    /// <param name="definition">The workflow definition to materialize the graph for. May be null.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A result containing the materialized workflow graph and its corresponding workflow definition, if successfully materialized; otherwise, returns the definition with a null graph.</returns>
    private async Task<WorkflowGraphFindResult> MaterializeWorkflowGraphFindResultAsync(WorkflowDefinition? definition, CancellationToken cancellationToken)
    {
        if (definition == null)
            return new(null, null);

        if (materializerRegistry.IsMaterializerAvailable(definition.MaterializerName))
        {
            var graph = await MaterializeWorkflowAsync(definition, cancellationToken);
            return new(definition, graph);
        }

        return new(definition, null);
    }
}