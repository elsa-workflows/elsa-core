using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Management.Mappers;

/// <summary>
/// Maps a workflow state to a workflow instance.
/// </summary>
public class WorkflowStateMapper
{
    /// <summary>
    /// [Obsolete] The property key name used to store the workflow instance name.
    /// </summary>
    [Obsolete("This constant is obsolete and retained only for backward compatibility. Avoid using it in new code.")]
    private const string WorkflowInstanceNameKey = "WorkflowInstanceName";
    
    /// <summary>
    /// Maps a workflow state to a workflow instance.
    /// </summary>
    public WorkflowInstance? Map(WorkflowState? source)
    {
        if (source == null)
            return null;

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
        target.TenantId = source.TenantId;
        target.CreatedAt = source.CreatedAt;
        target.DefinitionId = source.DefinitionId;
        target.DefinitionVersionId = source.DefinitionVersionId;
        target.Version = source.DefinitionVersion;
        target.ParentWorkflowInstanceId = source.ParentWorkflowInstanceId;
        target.Status = source.Status;
        target.SubStatus = source.SubStatus;
        target.IsExecuting = source.IsExecuting;
        target.CorrelationId = source.CorrelationId;
        target.Name = source.Name;
        target.IncidentCount = source.Incidents.Count;
        target.IsSystem = source.IsSystem;
        target.UpdatedAt = source.UpdatedAt;
        target.FinishedAt = source.FinishedAt;
        target.WorkflowState = source;
        
        // Keep for backward compatibility with workflow instances created before the introduction of the Name property.
        if (source.Properties.TryGetValue<string>(WorkflowInstanceNameKey, out var name))
            target.Name = name;
    }

    /// <summary>
    /// Maps a workflow instance to a workflow state.
    /// </summary>
    public WorkflowState? Map(WorkflowInstance? source)
    {
        if (source == null)
            return null;

        var workflowState = source.WorkflowState;
        workflowState.Id = source.Id;
        workflowState.TenantId = source.TenantId;
        workflowState.CreatedAt = source.CreatedAt;
        workflowState.DefinitionId = source.DefinitionId;
        workflowState.DefinitionVersionId = source.DefinitionVersionId;
        workflowState.DefinitionVersion = source.Version;
        workflowState.ParentWorkflowInstanceId = source.ParentWorkflowInstanceId;
        workflowState.Status = source.Status;
        workflowState.SubStatus = source.SubStatus;
        workflowState.IsExecuting = source.IsExecuting;
        workflowState.CorrelationId = source.CorrelationId;
        workflowState.Name = source.Name;
        workflowState.UpdatedAt = source.UpdatedAt;
        workflowState.FinishedAt = source.FinishedAt;
        workflowState.IsSystem = source.IsSystem;

        return workflowState;
    }
}