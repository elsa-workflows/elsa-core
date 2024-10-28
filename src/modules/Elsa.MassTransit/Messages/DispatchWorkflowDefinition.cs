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

    /// The ID of the workflow definition to dispatch.
    public string? DefinitionId { get; init; }

    /// The version options to use when dispatching the workflow definition.
    public VersionOptions? VersionOptions { get; init; }
    
    /// The ID of the workflow definition version to dispatch.
    public string? DefinitionVersionId { get; set; }

    /// The ID of the parent workflow instance.
    public string? ParentWorkflowInstanceId { get; init; }

    /// Deprecated. Use the <see cref="SerializedInput"/> property instead.
    [Obsolete("This property is no longer used and will be removed in a future version. Use the SerializedInput property instead.")]
    public IDictionary<string, object>? Input { get; set; }

    /// <summary>
    /// Any input to pass to the workflow.
    /// </summary>
    public string? SerializedInput { get; set; }

    /// Any properties to attach to the workflow.
    public IDictionary<string, object>? Properties { get; init; }

    /// A correlation ID to associate the workflow with.
    public string? CorrelationId { get; init; }

    /// The ID to use when creating an instance of the workflow to dispatch, or the ID of an existing instance to dispatch.
    public string? InstanceId { get; init; }

    /// Whether the instance is an existing instance. If it is, no new instance will be created.
    public bool IsExistingInstance { get; init; }

    /// The ID of the activity that triggered the workflow.
    public string? TriggerActivityId { get; init; }
}