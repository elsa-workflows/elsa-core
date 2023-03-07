using Elsa.Common.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IActivityWalker _activityWalker;
    private readonly IIdentityGraphService _identityGraphService;
    private readonly IEnumerable<IWorkflowMaterializer> _materializers;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowDefinitionService(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IActivityWalker activityWalker,
        IIdentityGraphService identityGraphService,
        IEnumerable<IWorkflowMaterializer> materializers)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _activityWalker = activityWalker;
        _identityGraphService = identityGraphService;
        _materializers = materializers;
    }

    /// <inheritdoc />
    public async Task<Workflow> MaterializeWorkflowAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        var provider = _materializers.FirstOrDefault(x => x.Name == definition.MaterializerName);

        if (provider == null)
            throw new Exception("Provider not found");

        var workflow = await provider.MaterializeAsync(definition, cancellationToken);
        var graph = (await _activityWalker.WalkAsync(workflow, cancellationToken)).Flatten().ToList();

        // Assign identities.
        _identityGraphService.AssignIdentities(graph);

        return workflow;
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = definitionId, VersionOptions = versionOptions };
        return await _workflowDefinitionStore.FindAsync(filter, cancellationToken);
    }
}