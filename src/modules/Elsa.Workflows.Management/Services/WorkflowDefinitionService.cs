using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IActivityVisitor _activityVisitor;
    private readonly IIdentityGraphService _identityGraphService;
    private readonly Func<IEnumerable<IWorkflowMaterializer>> _materializers;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowDefinitionService(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IActivityVisitor activityVisitor,
        IIdentityGraphService identityGraphService,
        Func<IEnumerable<IWorkflowMaterializer>> materializers)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _activityVisitor = activityVisitor;
        _identityGraphService = identityGraphService;
        _materializers = materializers;
    }

    /// <inheritdoc />
    public async Task<Workflow> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var materializers = _materializers();
        var materializer = materializers.FirstOrDefault(x => x.Name == definition.MaterializerName);

        if (materializer == null)
            throw new Exception("Provider not found");

        var workflow = await materializer.MaterializeAsync(definition, cancellationToken);
        var graph = (await _activityVisitor.VisitAsync(workflow, cancellationToken)).Flatten().ToList();

        _identityGraphService.AssignIdentities(graph);

        return workflow;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = versionOptions };
        return await _workflowDefinitionStore.FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { Id = definitionVersionId };
        return await _workflowDefinitionStore.FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindWorkflowDefinitionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        return await _workflowDefinitionStore.FindAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Workflow?> FindWorkflowAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionId, versionOptions, cancellationToken);

        if (definition == null)
            return null;

        return await MaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Workflow?> FindWorkflowAsync(string definitionVersionId, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(definitionVersionId, cancellationToken);

        if (definition == null)
            return null;

        return await MaterializeWorkflowAsync(definition, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Workflow?> FindWorkflowAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default)
    {
        var definition = await FindWorkflowDefinitionAsync(filter, cancellationToken);

        if (definition == null)
            return null;

        return await MaterializeWorkflowAsync(definition, cancellationToken);
    }
}