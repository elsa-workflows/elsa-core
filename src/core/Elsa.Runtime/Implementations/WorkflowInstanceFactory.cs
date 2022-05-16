using Elsa.Management.Services;
using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Persistence.Models;
using Elsa.Persistence.Services;
using Elsa.Runtime.Services;
using Elsa.Services;
using Elsa.State;

namespace Elsa.Runtime.Implementations;

public class WorkflowInstanceFactory : IWorkflowInstanceFactory
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore;
    private readonly IWorkflowMaterializer _workflowMaterializer;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ISystemClock _systemClock;

    public WorkflowInstanceFactory(
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowMaterializer workflowMaterializer,
        IIdentityGenerator identityGenerator,
        ISystemClock systemClock)
    {
        _workflowDefinitionStore = workflowDefinitionStore;
        _workflowMaterializer = workflowMaterializer;
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
        var workflow = await _workflowMaterializer.MaterializeAsync(definition, cancellationToken);
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