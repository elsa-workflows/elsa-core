using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Api.Client.Resources.WorkflowInstances.Models;

/// <summary>
/// Represents a workflow instance.
/// </summary>
public class WorkflowInstance : Entity
{
    /// <summary>
    /// The ID of the workflow definition.
    /// </summary>
    public string DefinitionId { get; set; } = default!;
    
    /// <summary>
    /// The version ID of the workflow definition.
    /// </summary>
    public string DefinitionVersionId { get; set; } = default!;
    
    /// <summary>
    /// The version of the workflow definition.
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// The state of the workflow instance.
    /// </summary>
    public WorkflowState WorkflowState { get; set; } = default!;
    
    /// <summary>
    /// The status of the workflow instance.
    /// </summary>
    public WorkflowStatus Status { get; set; }
    
    /// <summary>
    /// The sub-status of the workflow instance.
    /// </summary>
    public WorkflowSubStatus SubStatus { get; set; }
    
    /// <summary>
    /// The ID of the workflow instance.
    /// </summary>
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// The name of the workflow instance.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The timestamp when the workflow instance was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    
    /// <summary>
    /// The timestamp when the workflow instance was last executed.
    /// </summary>
    public DateTimeOffset? LastExecutedAt { get; set; }
    
    /// <summary>
    /// The timestamp when the workflow instance was finished.
    /// </summary>
    public DateTimeOffset? FinishedAt { get; set; }
    
    /// <summary>
    /// The timestamp when the workflow instance was cancelled.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; set; }
    
    /// <summary>
    /// The timestamp when the workflow instance was faulted.
    /// </summary>
    public DateTimeOffset? FaultedAt { get; set; }
}