using Elsa.Common.Contracts;
using Elsa.Common.Models;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class WorkflowInstanceFactory(
    IWorkflowDefinitionStore workflowDefinitionStore,
    IWorkflowDefinitionService workflowDefinitionService,
    IIdentityGenerator identityGenerator,
    ISystemClock systemClock)
    : IWorkflowInstanceFactory
{
    /// <inheritdoc />
    public async Task<WorkflowInstance> CreateAsync(string workflowDefinitionId, string? correlationId, CancellationToken cancellationToken = default)
    {
        return await CreateAsync(workflowDefinitionId, VersionOptions.Published, correlationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> CreateAsync(string workflowDefinitionId, VersionOptions versionOptions, string? correlationId, CancellationToken cancellationToken = default)
    {
        var filter = new WorkflowDefinitionFilter
        {
            DefinitionId = workflowDefinitionId,
            VersionOptions = versionOptions
        };
        var workflow = (await workflowDefinitionStore.FindAsync(filter, cancellationToken))!;
        return await CreateAsync(workflow, correlationId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowInstance> CreateAsync(WorkflowDefinition definition, string? correlationId, CancellationToken cancellationToken = default)
    {
        var workflow = await workflowDefinitionService.MaterializeWorkflowAsync(definition, cancellationToken);
        return Create(workflow, correlationId);
    }
    
    /// <inheritdoc />
    public WorkflowInstance Create(Workflow workflow, string? correlationId)
    {
        var workflowInstanceId = identityGenerator.GenerateId();

        var workflowInstance = new WorkflowInstance
        {
            Id = workflowInstanceId,
            Version = workflow.Identity.Version,
            DefinitionId = workflow.Identity.DefinitionId,
            DefinitionVersionId = workflow.Identity.Id,
            CorrelationId = correlationId,
            TenantId = workflow.Identity.TenantId,
            CreatedAt = systemClock.UtcNow,
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