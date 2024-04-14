using Elsa.Common.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Params;
using Elsa.Workflows.Management.Requests;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Management.Services;

/// <inheritdoc />
public class WorkflowInstanceFactory(IIdentityGenerator identityGenerator, ISystemClock systemClock) : IWorkflowInstanceFactory
{
    /// <inheritdoc />
    public WorkflowState CreateWorkflowState(CreateWorkflowInstanceParams @params)
    {
        var now = systemClock.UtcNow;
        return new WorkflowState
        {
            Id = @params.WorkflowInstanceId ?? identityGenerator.GenerateId(),
            DefinitionId = @params.Workflow.Identity.DefinitionId,
            DefinitionVersionId = @params.Workflow.Identity.Id,
            DefinitionVersion = @params.Workflow.Identity.Version,
            CorrelationId = @params.CorrelationId,
            Input = @params.Input ?? new Dictionary<string, object>(),
            Properties = @params.Properties ?? new Dictionary<string, object>(),
            Status = WorkflowStatus.Running,
            SubStatus = WorkflowSubStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            ParentWorkflowInstanceId = @params.ParentId,
            IsSystem = @params.Workflow.IsSystem
        };
    }

    /// <inheritdoc />
    public WorkflowInstance CreateWorkflowInstance(CreateWorkflowInstanceParams @params)
    {
        var workflowState = CreateWorkflowState(@params);
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