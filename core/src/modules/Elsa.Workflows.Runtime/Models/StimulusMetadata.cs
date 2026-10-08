using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents information about how to route a stimulus to an activity and workflow instance.
/// </summary>
[UsedImplicitly]
public class StimulusMetadata
{
    /// <summary>
    /// The ID of the workflow instance to route the stimulus to.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// The correlation ID of the workflow instance to route the stimulus to.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The handle of the activity to route the stimulus to.
    /// </summary>
    public string? ActivityInstanceId { get; set; }

    /// <summary>
    /// The ID of the bookmark created by the activity to route the stimulus to.
    /// </summary>
    public string? BookmarkId { get; set; }
    
    /// <summary>
    /// The ID of the workflow instance that sent the stimulus.
    /// </summary>
    public string? ParentWorkflowInstanceId { get; set; }
    
    /// <summary>
    /// Any input that was provided by the sender.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }
    
    /// <summary>
    /// Any properties that were provided by the sender.
    /// </summary>
    public IDictionary<string, object>? Properties { get; set; }
}