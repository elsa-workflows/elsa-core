using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowInstanceFactory : IWorkflowInstanceFactory
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _systemClock;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowInstanceFactory(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowDefinitionService workflowDefinitionService,
        IIdentityGenerator identityGenerator,
        ISystemClock systemClock)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowDefinitionService = workflowDefinitionService;
        _identityGenerator = identityGenerator;
        _systemClock = systemClock;
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> CreateAsync(string workflowDefinitionId, string? correlationId, CancellationToken cancellationToken = default) =>
        await CreateAsync(workflowDefinitionId, VersionOptions.Published, correlationId, cancellationToken);

    /// <inheritdoc />
    public async Task<WorkflowInstance> CreateAsync(string workflowDefinitionId, VersionOptions versionOptions, string? correlationId, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter { DefinitionId = workflowDefinitionId, VersionOptions = versionOptions};
        var workflow = (await _workflowDefinitionStore.FindAsync(filter, cancellationToken))!;
        return await CreateAsync(workflow, correlationId, cancellationToken);
    }

    /// <inheritdoc />
    public WorkflowInstance Create(Workflow workflow, string? correlationId)
    {
        var workflowInstanceId = _identityGenerator.GenerateId();

        var workflowInstance = new WorkflowInstance
        {
            Id = workflowInstanceId,
            Version = workflow.Identity.Version,
            DefinitionId = workflow.Identity.DefinitionId,
            DefinitionVersionId = workflow.Identity.Id,
            CorrelationId = correlationId,
            CreatedAt = _systemClock.UtcNow,
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Executing,
            WorkflowState = new WorkflowState
            {
                Id = workflowInstanceId,
                Status = WorkflowStatus.Running,
                SubStatus = WorkflowSubStatus.Executing,
                CorrelationId = correlationId
            }
        };

        return workflowInstance;
    }
    
    private async Task<WorkflowInstance> CreateAsync(WorkflowDefinition definition, string? correlationId, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        return Create(workflow, correlationId);
    }
}