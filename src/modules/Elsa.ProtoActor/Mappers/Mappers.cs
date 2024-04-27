using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.State;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Contains all mappers used by the ProtoActor implementation.
/// </summary>
public class Mappers(
    ActivityHandleMapper activityHandleMapper,
    WorkflowDefinitionHandleMapper workflowDefinitionHandleMapper,
    ActivityIncidentMapper activityIncidentMapper,
    ExceptionMapper exceptionMapper,
    WorkflowStatusMapper workflowStatusMapper,
    WorkflowSubStatusMapper workflowSubStatusMapper,
    CreateWorkflowInstanceRequestMapper createWorkflowInstanceRequestMapper,
    CreateWorkflowInstanceResponseMapper createWorkflowInstanceResponseMapper,
    RunWorkflowInstanceRequestMapper runWorkflowInstanceRequestMapper,
    RunWorkflowInstanceResponseMapper runWorkflowInstanceResponseMapper,
    RunWorkflowParamsMapper runWorkflowParamsMapper,
    WorkflowStateJsonMapper workflowStateJsonMapper)
{
    /// <summary>
    /// Maps between <see cref="ActivityHandle"/> and <see cref="ProtoActivityHandle"/>.
    /// </summary>
    public ActivityHandleMapper ActivityHandleMapper { get; } = activityHandleMapper;

    /// <summary>
    /// Maps between <see cref="WorkflowDefinitionHandle"/> and <see cref="ProtoWorkflowDefinitionHandle"/>.
    /// </summary>
    public WorkflowDefinitionHandleMapper WorkflowDefinitionHandleMapper { get; } = workflowDefinitionHandleMapper;

    /// <summary>
    /// Maps between <see cref="ActivityIncident"/> and <see cref="ProtoActivityIncident"/>.
    /// </summary>
    public ActivityIncidentMapper ActivityIncidentMapper { get; } = activityIncidentMapper;

    /// <summary>
    /// Maps between <see cref="ExceptionState"/> and <see cref="ProtoExceptionState"/>.
    /// </summary>
    public ExceptionMapper ExceptionMapper { get; } = exceptionMapper;

    /// <summary>
    /// Maps between <see cref="WorkflowStatus"/> and <see cref="ProtoWorkflowStatus"/>.
    /// </summary>
    public WorkflowStatusMapper WorkflowStatusMapper { get; } = workflowStatusMapper;

    /// <summary>
    /// Maps between <see cref="WorkflowSubStatus"/> and <see cref="ProtoWorkflowSubStatus"/>.
    /// </summary>
    public WorkflowSubStatusMapper WorkflowSubStatusMapper { get; } = workflowSubStatusMapper;

    /// <summary>
    /// Maps between <see cref="CreateWorkflowInstanceRequest"/> and <see cref="ProtoCreateWorkflowInstanceRequest"/>.
    /// </summary>
    public CreateWorkflowInstanceRequestMapper CreateWorkflowInstanceRequestMapper { get; } = createWorkflowInstanceRequestMapper;

    /// <summary>
    /// Maps between <see cref="CreateWorkflowInstanceResponse"/> and <see cref="ProtoCreateWorkflowInstanceResponse"/>.
    /// </summary>
    public CreateWorkflowInstanceResponseMapper CreateWorkflowInstanceResponseMapper { get; } = createWorkflowInstanceResponseMapper;

    /// <summary>
    /// Maps between <see cref="RunWorkflowInstanceRequest"/> and <see cref="ProtoRunWorkflowInstanceRequest"/>.
    /// </summary>
    public RunWorkflowInstanceRequestMapper RunWorkflowInstanceRequestMapper { get; } = runWorkflowInstanceRequestMapper;

    /// <summary>
    /// Maps between <see cref="RunWorkflowResult"/> and <see cref="ProtoRunWorkflowInstanceResponse"/>.
    /// </summary>
    public RunWorkflowInstanceResponseMapper RunWorkflowInstanceResponseMapper { get; set; } = runWorkflowInstanceResponseMapper;

    /// <summary>
    /// Maps between <see cref="RunWorkflowParams"/> and <see cref="ProtoRunWorkflowInstanceRequest"/>.
    /// </summary>
    public RunWorkflowParamsMapper RunWorkflowParamsMapper { get; } = runWorkflowParamsMapper;

    /// <summary>
    /// Maps between <see cref="WorkflowState"/> and <see cref="ProtoJson"/>.
    /// </summary>
    public WorkflowStateJsonMapper WorkflowStateJsonMapper { get; } = workflowStateJsonMapper;
}