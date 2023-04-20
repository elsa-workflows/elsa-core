using Elsa.ProtoActor.Extensions;
using Elsa.Workflows.Runtime.Contracts;
using ProtoWorkflowExecutionResponse = Elsa.ProtoActor.Protos.WorkflowExecutionResponse;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps between <see cref="WorkflowExecutionResult"/> and <see cref="ProtoWorkflowExecutionResponse"/>.
/// </summary>
public class WorkflowExecutionResultMapper
{
    private readonly WorkflowStatusMapper _workflowStatusMapper;
    private readonly WorkflowSubStatusMapper _workflowSubStatusMapper;
    private readonly BookmarkMapper _bookmarkMapper;
    private readonly WorkflowFaultStateMapper _workflowFaultStateMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowExecutionResultMapper"/> class.
    /// </summary>
    public WorkflowExecutionResultMapper(
        WorkflowStatusMapper workflowStatusMapper, 
        WorkflowSubStatusMapper workflowSubStatusMapper,
        BookmarkMapper bookmarkMapper, 
        WorkflowFaultStateMapper workflowFaultStateMapper)
    {
        _workflowStatusMapper = workflowStatusMapper;
        _workflowSubStatusMapper = workflowSubStatusMapper;
        _bookmarkMapper = bookmarkMapper;
        _workflowFaultStateMapper = workflowFaultStateMapper;
    }
    
    /// <summary>
    /// Maps a <see cref="ProtoWorkflowExecutionResponse"/> to a <see cref="WorkflowExecutionResult"/>.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>The mapped <see cref="WorkflowExecutionResult"/>.</returns>
    public WorkflowExecutionResult Map(ProtoWorkflowExecutionResponse source) => new(
        source.WorkflowInstanceId,
         _workflowStatusMapper.Map(source.Status),
        _workflowSubStatusMapper.Map(source.SubStatus),
        _bookmarkMapper.Map(source.Bookmarks).ToList(),
        source.TriggeredActivityId.NullIfEmpty(),
        source.Fault != null ? _workflowFaultStateMapper.Map(source.Fault) : default);
    
    /// <summary>
    /// Maps a <see cref="WorkflowExecutionResult"/> to a <see cref="ProtoWorkflowExecutionResponse"/>.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>The mapped <see cref="ProtoWorkflowExecutionResponse"/>.</returns>
    public ProtoWorkflowExecutionResponse Map(WorkflowExecutionResult source) => new()
    {
        WorkflowInstanceId = source.WorkflowInstanceId,
        Status = _workflowStatusMapper.Map(source.Status),
        SubStatus = _workflowSubStatusMapper.Map(source.SubStatus),
        Bookmarks = {_bookmarkMapper.Map(source.Bookmarks)},
        TriggeredActivityId = source.TriggeredActivityId,
        Fault = source.Fault != null ? _workflowFaultStateMapper.Map(source.Fault) : default
    };
}