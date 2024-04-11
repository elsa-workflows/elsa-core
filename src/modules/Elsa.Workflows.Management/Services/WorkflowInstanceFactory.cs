using Elsa.Common.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Requests;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowInstanceFactory(IIdentityGenerator identityGenerator, ISystemClock systemClock) : IWorkflowInstanceFactory
{
    /// <inheritdoc />
    public WorkflowState CreateWorkflowState(CreateWorkflowInstanceRequest request)
    {
        var now = systemClock.UtcNow;
        return new WorkflowState
        {
            Id = request.WorkflowInstanceId ?? identityGenerator.GenerateId(),
            DefinitionId = request.Workflow.Identity.DefinitionId,
            DefinitionVersionId = request.Workflow.Identity.Id,
            DefinitionVersion = request.Workflow.Identity.Version,
            CorrelationId = request.CorrelationId,
            Input = request.Input ?? new Dictionary<string, object>(),
            Properties = request.Properties ?? new Dictionary<string, object>(),
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            ParentWorkflowInstanceId = request.ParentWorkflowInstanceId,
            IsSystem = request.Workflow.IsSystem
        };
    }

    /// <inheritdoc />
    public WorkflowInstance CreateWorkflowInstance(CreateWorkflowInstanceRequest request)
    {
        var workflowState = CreateWorkflowState(request);
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