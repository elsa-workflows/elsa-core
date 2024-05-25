using Elsa.Common.Models;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionService(
    IWorkflowDefinitionStore workflowDefinitionStore,
    IWorkflowGraphBuilder workflowGraphBuilder,
    Func<IEnumerable<IWorkflowMaterializer>> materializers)
    : IWorkflowDefinitionService
{
    /// <inheritdoc />
    public async Task<WorkflowGraph> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var workflowMaterializers = materializers();
        var materializer = workflowMaterializers.FirstOrDefault(x => x.Name == definition.MaterializerName);

        if (materializer == null)
            throw new Exception("Provider not found");

        var workflow = await materializer.MaterializeAsync(definition, cancellationToken);
        return await workflowGraphBuilder.BuildAsync(workflow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionId, VersionOptions versionOptions, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = definitionId,
            VersionOptions = versionOptions,
            TenantAgnostic = tenantAgnostic
        };
        return await workflowDefinitionStore.FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionVersionId, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter
        {
            Id = definitionVersionId,
            TenantAgnostic = tenantAgnostic
        };
        return await workflowDefinitionStore.FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await workflowDefinitionStore.FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionId, VersionOptions versionOptions, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionId, versionOptions, tenantAgnostic, cancellationToken);

        if (definition == null)
            return null;

        return await MaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(string definitionVersionId, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionVersionId, tenantAgnostic, cancellationToken);

        if (definition == null)
            return null;

        return await MaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowGraph?> FindWorkflowGraphAsync(WorkflowDefinitionFilter filter, bool tenantAgnostic, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(filter, cancellationToken);

        if (definition == null)
            return null;

        return await MaterializeWorkflowAsync(definition, cancellationToken);
    }
}