using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Persistence.Entities;
using Elsa.Workflows.Persistence.Models;
using Elsa.Workflows.Persistence.Services;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Implementations;

public class WorkflowInstanceFactory : IWorkflowInstanceFactory
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _systemClock;

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

    public async Task<WorkflowInstance> CreateAsync(string workflowDefinitionId, string? correlationId, CancellationToken cancellationToken = default) =>
        await CreateAsync(workflowDefinitionId, VersionOptions.Published, correlationId, cancellationToken);

    public async Task<WorkflowInstance> CreateAsync(string workflowDefinitionId, VersionOptions versionOptions, string? correlationId, CancellationToken cancellationToken = default)
    {
        var workflow = (await _workflowDefinitionStore.FindByDefinitionIdAsync(workflowDefinitionId, versionOptions, cancellationToken))!;
        return await CreateAsync(workflow, correlationId, cancellationToken);
    }

    public async Task<WorkflowInstance> CreateAsync(WorkflowDefinition definition, string? correlationId, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        return Create(workflow, correlationId);
    }

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
}