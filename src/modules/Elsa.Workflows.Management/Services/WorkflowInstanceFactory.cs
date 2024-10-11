using Elsa.Common;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowInstanceFactory(IIdentityGenerator identityGenerator, ISystemClock systemClock) : IWorkflowInstanceFactory
{
    /// <inheritdoc />
    public WorkflowState CreateWorkflowState(Workflow workflow, WorkflowInstanceOptions? options = null)
    {
        var now = systemClock.UtcNow;
        return new WorkflowState
        {
            Id = string.IsNullOrEmpty(options?.WorkflowInstanceId) ? identityGenerator.GenerateId() : options.WorkflowInstanceId,
            DefinitionId = workflow.Identity.DefinitionId,
            DefinitionVersionId = workflow.Identity.Id,
            DefinitionVersion = workflow.Identity.Version,
            CorrelationId = options?.CorrelationId,
            Input = options?.Input ?? new Dictionary<string, object>(),
            Properties = options?.Properties ?? new Dictionary<string, object>(),
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            ParentWorkflowInstanceId = options?.ParentWorkflowInstanceId,
            IsSystem = workflow.IsSystem
        };
    }

    /// <inheritdoc />
    public WorkflowInstance CreateWorkflowInstance(Workflow workflow, WorkflowInstanceOptions? options = null)
    {
        var workflowState = CreateWorkflowState(workflow, options);
        return new WorkflowInstance
        {
            Id = workflowState.Id,
            ParentWorkflowInstanceId = workflowState.ParentWorkflowInstanceId,
            WorkflowState = workflowState,
            DefinitionId = workflowState.DefinitionId,
            DefinitionVersionId = workflowState.DefinitionVersionId,
            Version = workflowState.DefinitionVersion,
            CorrelationId = workflowState.CorrelationId,
            Status = workflowState.Status,
            SubStatus = workflowState.SubStatus,
            IncidentCount = workflowState.Incidents.Count,
            IsSystem = workflowState.IsSystem,
            CreatedAt = workflowState.CreatedAt,
            UpdatedAt = workflowState.UpdatedAt,
            FinishedAt = workflowState.FinishedAt,
        };
    }
}