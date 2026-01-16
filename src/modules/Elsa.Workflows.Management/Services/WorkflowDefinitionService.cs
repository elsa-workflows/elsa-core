using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionService(
    IWorkflowDefinitionStore workflowDefinitionStore,
    IWorkflowGraphBuilder workflowGraphBuilder,
    IMaterializerRegistry materializerRegistry)
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

        if (definition == null)
            return null;

        return await MaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionVersionId, cancellationToken);

        if (definition == null)
            return null;

        return await MaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(WorkflowDefinitionHandle definitionHandle, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionHandle, cancellationToken);

        if (definition == null)
            return null;

        return await MaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(filter, cancellationToken);

        if (definition == null)
            return null;

        return await MaterializeWorkflowAsync(definition, cancellationToken);
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
}