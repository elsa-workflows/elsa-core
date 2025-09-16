using Elsa.Common.Entities;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Management.Entities;

/// <summary>
/// Represents a workflow instance.
/// </summary>
public class WorkflowInstance : Entity
{
    /// <summary>
    /// The ID of the workflow definition.
    /// </summary>
    public string DefinitionId { get; set; } = null!;
    
    /// <summary>
    /// The version ID of the workflow definition.
    /// </summary>
    public string DefinitionVersionId { get; set; } = null!;
    
    /// <summary>
    /// The version of the workflow definition.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// The ID of the parent workflow.
    /// </summary>
    public string? ParentWorkflowInstanceId { get; set; }
    
    /// <summary>
    /// The state of the workflow instance.
    /// </summary>
    public WorkflowState WorkflowState { get; set; } = null!;
    
    /// <summary>
    /// The status of the workflow instance.
    /// </summary>
    public WorkflowStatus Status { get; set; }
    
    /// <summary>
    /// The sub-status of the workflow instance.
    /// </summary>
    public WorkflowSubStatus SubStatus { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the workflow instance is actively executing. 
    /// </summary>
    /// <remarks>
    /// This flag is set to <c>true</c> immediately before the workflow begins execution 
    /// and is set to <c>false</c> once the execution is completed. 
    /// It can be used to determine if a workflow instance was in-progress in case of unexpected 
    /// application termination, allowing the system to retry execution upon restarting. 
    /// </remarks>
    public bool IsExecuting { get; set; }
    
    /// <summary>
    /// The ID of the workflow instance.
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// The name of the workflow instance.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The number of incidents that have occurred during execution, if any.
    /// </summary>
    public int IncidentCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the workflow instance is a system workflow.
    /// </summary>
    public bool IsSystem { get; set; }
    
    /// <summary>
    /// The timestamp when the workflow instance was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// The timestamp when the workflow instance was last executed.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
    
    /// <summary>
    /// The timestamp when the workflow instance was finished.
    /// </summary>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>
    /// The name of the user who executed the workflow instance
    /// </summary>
    public string? Initiator { get; set; }
}