using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.State;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Contains all mappers used by the ProtoActor implementation.
/// </summary>
public class Mappers(
    ActivityHandleMapper activityHandleMapper,
    ActivityIncidentMapper activityIncidentMapper,
    BookmarkMapper bookmarkMapper,
    BookmarkInfoMapper bookmarkInfoMapper,
    BookmarkDiffMapper bookmarkDiffMapper,
    ExceptionMapper exceptionMapper,
    WorkflowStatusMapper workflowStatusMapper,
    WorkflowSubStatusMapper workflowSubStatusMapper,
    ExecuteWorkflowRequestMapper executeWorkflowRequestMapper,
    ExecuteWorkflowResponseMapper executeWorkflowResponseMapper,
    WorkflowStateJsonMapper workflowStateJsonMapper)
{
    /// <summary>
    /// Maps between <see cref="ActivityHandle"/> and <see cref="ProtoActivityHandle"/>.
    /// </summary>
    public ActivityHandleMapper ActivityHandleMapper { get; } = activityHandleMapper;

    /// <summary>
    /// Maps between <see cref="ActivityIncident"/> and <see cref="ProtoActivityIncident"/>.
    /// </summary>
    public ActivityIncidentMapper ActivityIncidentMapper { get; } = activityIncidentMapper;

    /// <summary>
    /// Maps between <see cref="Bookmark"/> and <see cref="ProtoBookmark"/>.
    /// </summary>
    public BookmarkMapper BookmarkMapper { get; } = bookmarkMapper;

    /// <summary>
    /// Maps between <see cref="BookmarkInfo"/> and <see cref="ProtoBookmarkInfo"/>.
    /// </summary>
    public BookmarkInfoMapper BookmarkInfoMapper { get; } = bookmarkInfoMapper;

    /// <summary>
    /// Maps between <see cref="Diff{T}"/> and <see cref="ProtoBookmarkDiff"/>.
    /// </summary>
    public BookmarkDiffMapper BookmarkDiffMapper { get; } = bookmarkDiffMapper;

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
    /// Maps between <see cref="IExecuteWorkflowRequest"/> and <see cref="ProtoExecuteWorkflowRequest"/>.
    /// </summary>
    public ExecuteWorkflowRequestMapper ExecuteWorkflowRequestMapper { get; } = executeWorkflowRequestMapper;

    /// <summary>
    /// Maps between <see cref="IExecuteWorkflowRequest"/> and <see cref="ProtoExecuteWorkflowRequest"/>.
    /// </summary>
    public ExecuteWorkflowResponseMapper ExecuteWorkflowResponseMapper { get; set; } = executeWorkflowResponseMapper;

    /// <summary>
    /// Maps between <see cref="WorkflowState"/> and <see cref="ProtoJson"/>.
    /// </summary>
    public WorkflowStateJsonMapper WorkflowStateJsonMapper { get; } = workflowStateJsonMapper;
}