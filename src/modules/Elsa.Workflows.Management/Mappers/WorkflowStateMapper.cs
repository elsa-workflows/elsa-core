using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Management.Mappers;

/// <summary>
/// Maps a workflow state to a workflow instance.
/// </summary>
public class WorkflowStateMapper
{
    /// <summary>
    /// Maps a workflow state to a workflow instance.
    /// </summary>
    public WorkflowInstance? Map(WorkflowState? source)
    {
        if (source == null)
            return default;

        var workflowInstance = new WorkflowInstance
        {
            Id = source.Id,
            CreatedAt = source.CreatedAt,
            DefinitionId = source.DefinitionId,
            DefinitionVersionId = source.DefinitionVersionId,
            Version = source.DefinitionVersion,
            Status = source.Status,
            SubStatus = source.SubStatus,
            CorrelationId = source.CorrelationId,
            IncidentCount = source.Incidents.Count,
            UpdatedAt = source.UpdatedAt,
            FinishedAt = source.FinishedAt,
            WorkflowState = source
        };
        
        if (source.Properties.TryGetValue<string>(SetName.WorkflowInstanceNameKey, out var name))
            workflowInstance.Name = name;

        return workflowInstance;
    }

    /// <summary>
    /// Maps a workflow instance to a workflow state.
    /// </summary>
    public WorkflowState? Map(WorkflowInstance? source)
    {
        if (source == null)
            return default;

        var workflowState = source.WorkflowState;
        workflowState.Id = source.Id;
        workflowState.CreatedAt = source.CreatedAt;
        workflowState.DefinitionId = source.DefinitionId;
        workflowState.DefinitionVersionId = source.DefinitionVersionId;
        workflowState.DefinitionVersion = source.Version;
        workflowState.Status = source.Status;
        workflowState.SubStatus = source.SubStatus;
        workflowState.CorrelationId = source.CorrelationId;
        workflowState.UpdatedAt = source.UpdatedAt;
        workflowState.FinishedAt = source.FinishedAt;

        if (source.Name != null)
            workflowState.Properties[SetName.WorkflowInstanceNameKey] = source.Name;

        return workflowState;
    }
}