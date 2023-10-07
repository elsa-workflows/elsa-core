using Elsa.ProtoActor.Extensions;
using Elsa.Workflows.Runtime.Results;
using ProtoWorkflowExecutionResponse = Elsa.ProtoActor.ProtoBuf.WorkflowExecutionResponse;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps between <see cref="WorkflowExecutionResult"/> and <see cref="ProtoWorkflowExecutionResponse"/>.
/// </summary>
internal class WorkflowExecutionResultMapper
{
    private readonly WorkflowStatusMapper _workflowStatusMapper;
    private readonly WorkflowSubStatusMapper _workflowSubStatusMapper;
    private readonly BookmarkMapper _bookmarkMapper;
    private readonly ActivityIncidentStateMapper _activityIncidentStateMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowExecutionResultMapper"/> class.
    /// </summary>
    public WorkflowExecutionResultMapper(
        WorkflowStatusMapper workflowStatusMapper,
        WorkflowSubStatusMapper workflowSubStatusMapper,
        BookmarkMapper bookmarkMapper,
        ActivityIncidentStateMapper activityIncidentStateMapper)
    {
        _workflowStatusMapper = workflowStatusMapper;
        _workflowSubStatusMapper = workflowSubStatusMapper;
        _bookmarkMapper = bookmarkMapper;
        _activityIncidentStateMapper = activityIncidentStateMapper;
    }

    /// <summary>
    /// Maps a <see cref="ProtoWorkflowExecutionResponse"/> to a <see cref="WorkflowExecutionResult"/>.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>The mapped <see cref="WorkflowExecutionResult"/>.</returns>
    public WorkflowExecutionResult Map(ProtoWorkflowExecutionResponse source)
    {
        return new WorkflowExecutionResult(
            source.WorkflowInstanceId,
            _workflowStatusMapper.Map(source.Status),
            _workflowSubStatusMapper.Map(source.SubStatus),
            _bookmarkMapper.Map(source.Bookmarks).ToList(),
            _activityIncidentStateMapper.Map(source.Incidents).ToList(),
            source.TriggeredActivityId.NullIfEmpty()
        );
    }

    /// <summary>
    /// Maps a <see cref="WorkflowExecutionResult"/> to a <see cref="ProtoWorkflowExecutionResponse"/>.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>The mapped <see cref="ProtoWorkflowExecutionResponse"/>.</returns>
    public ProtoWorkflowExecutionResponse Map(WorkflowExecutionResult source)
    {
        return new ProtoWorkflowExecutionResponse
        {
            WorkflowInstanceId = source.WorkflowInstanceId,
            Status = _workflowStatusMapper.Map(source.Status),
            SubStatus = _workflowSubStatusMapper.Map(source.SubStatus),
            Bookmarks = { _bookmarkMapper.Map(source.Bookmarks) },
            Incidents = { _activityIncidentStateMapper.Map(source.Incidents).ToList() },
            TriggeredActivityId = source.TriggeredActivityId,
        };
    }
}