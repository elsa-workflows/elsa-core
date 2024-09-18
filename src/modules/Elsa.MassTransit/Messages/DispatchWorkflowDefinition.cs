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
    /// A message to dispatch a workflow definition for execution.
    /// </summary>
    /// <param name="definitionId">The ID of the workflow definition to dispatch.</param>
    /// <param name="versionOptions">The version options to use when dispatching the workflow definition.</param>
    /// <param name="parentWorkflowInstanceId">The ID of the parent workflow instance.</param>
    /// <param name="input">Any input to pass to the workflow.</param>
    /// <param name="properties">Any properties to attach to the workflow.</param>
    /// <param name="correlationId">A correlation ID to associate the workflow with.</param>
    /// <param name="instanceId">The ID to use when creating an instance of the workflow to dispatch.</param>
    /// <param name="triggerActivityId">The ID of the activity that triggered the workflow.</param>
    public static DispatchWorkflowDefinition DispatchNewWorkflowInstance(
        string? definitionId,
        VersionOptions? versionOptions,
        string? parentWorkflowInstanceId,
        string? serializedInput,
        IDictionary<string, object>? properties,
        string? correlationId,
        string? instanceId,
        string? triggerActivityId)
    {
        return new()
        {
            DefinitionId = definitionId,
            VersionOptions = versionOptions,
            ParentWorkflowInstanceId = parentWorkflowInstanceId,
            SerializedInput = serializedInput,
            Properties = properties,
            CorrelationId = correlationId,
            InstanceId = instanceId,
            TriggerActivityId = triggerActivityId
        };
    }

    /// The ID of the workflow definition to dispatch.
    public string? DefinitionId { get; init; }

    /// The version options to use when dispatching the workflow definition.
    public VersionOptions? VersionOptions { get; init; }

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