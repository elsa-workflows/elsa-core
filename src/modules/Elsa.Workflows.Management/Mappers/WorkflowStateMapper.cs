using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.State;

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

        var workflowInstance = new WorkflowInstance();
        Apply(source, workflowInstance);

        return workflowInstance;
    }
    
    /// <summary>
    /// Maps a workflow state to a workflow instance.
    /// </summary>
    public void Apply(WorkflowState source, WorkflowInstance target)
    {
        target.Id = source.Id;
        target.CreatedAt = source.CreatedAt;
        target.DefinitionId = source.DefinitionId;
        target.DefinitionVersionId = source.DefinitionVersionId;
        target.Version = source.DefinitionVersion;
        target.ParentWorkflowInstanceId = source.ParentWorkflowInstanceId;
        target.Status = source.Status;
        target.SubStatus = source.SubStatus;
        target.CorrelationId = source.CorrelationId;
        target.IncidentCount = source.Incidents.Count;
        target.IsSystem = source.IsSystem;
        target.UpdatedAt = source.UpdatedAt;
        target.FinishedAt = source.FinishedAt;
        target.WorkflowState = source;
        
        if (source.Properties.TryGetValue<string>(SetName.WorkflowInstanceNameKey, out var name))
            target.Name = name;
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
        workflowState.ParentWorkflowInstanceId = source.ParentWorkflowInstanceId;
        workflowState.Status = source.Status;
        workflowState.SubStatus = source.SubStatus;
        workflowState.CorrelationId = source.CorrelationId;
        workflowState.UpdatedAt = source.UpdatedAt;
        workflowState.FinishedAt = source.FinishedAt;
        workflowState.IsSystem = source.IsSystem;

        if (source.Name != null)
            workflowState.Properties[SetName.WorkflowInstanceNameKey] = source.Name;

        return workflowState;
    }
}