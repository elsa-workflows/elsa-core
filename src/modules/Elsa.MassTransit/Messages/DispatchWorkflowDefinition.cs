using Elsa.Common.Models;

namespace Elsa.MassTransit.Messages;

/// <summary>
/// A message to dispatch a workflow definition for execution.
/// </summary>
public record DispatchWorkflowDefinition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DispatchWorkflowDefinition"/> class.
    /// </summary>
    public static DispatchWorkflowDefinition DispatchExistingWorkflowInstance(string instanceId, string? triggerActivityId)
    {
        return new()
        {
            InstanceId = instanceId,
            TriggerActivityId = triggerActivityId,
            IsExistingInstance = true
        };
    }

    /// <summary>
    /// The ID of the workflow definition to dispatch.
    /// </summary>
    public string? DefinitionId { get; init; }

    /// <summary>
    /// The version options to use when dispatching the workflow definition.
    /// </summary>
    public VersionOptions? VersionOptions { get; init; }
    
    /// <summary>
    /// The ID of the workflow definition version to dispatch.
    /// </summary>
    public string? DefinitionVersionId { get; set; }

    /// <summary>
    /// The ID of the parent workflow instance.
    /// </summary>
    public string? ParentWorkflowInstanceId { get; init; }

    /// Deprecated. Use the <see cref="SerializedInput"/> property instead.
    [Obsolete("This property is no longer used and will be removed in a future version. Use the SerializedInput property instead.")]
    public IDictionary<string, object>? Input { get; set; }

    /// <summary>
    /// Any input to pass to the workflow.
    /// </summary>
    public string? SerializedInput { get; set; }

    /// <summary>
    /// Any properties to attach to the workflow.
    /// </summary>
    public IDictionary<string, object>? Properties { get; init; }

    /// <summary>
    /// A correlation ID to associate the workflow with.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// The ID to use when creating an instance of the workflow to dispatch, or the ID of an existing instance to dispatch.
    /// </summary>
    public string? InstanceId { get; init; }

    /// <summary>
    /// Whether the instance is an existing instance. If it is, no new instance will be created.
    /// </summary>
    public bool IsExistingInstance { get; init; }

    /// <summary>
    /// The ID of the activity that triggered the workflow.
    /// </summary>
    public string? TriggerActivityId { get; init; }
}