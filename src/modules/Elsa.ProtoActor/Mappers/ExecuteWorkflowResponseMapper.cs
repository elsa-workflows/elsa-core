using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Results;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="IExecuteWorkflowRequest"/> to <see cref="ProtoExecuteWorkflowRequest"/>.
/// </summary>
public class ExecuteWorkflowResponseMapper(
    WorkflowStatusMapper workflowStatusMapper,
    WorkflowSubStatusMapper workflowSubStatusMapper,
    ActivityIncidentMapper activityIncidentMapper,
    BookmarkDiffMapper bookmarkDiffMapper)
{
    /// <summary>
    /// Maps <see cref="IExecuteWorkflowRequest"/> to <see cref="ProtoExecuteWorkflowRequest"/>.
    /// </summary>
    public ProtoExecuteWorkflowResponse Map(ExecuteWorkflowResult source)
    {
        var response = new ProtoExecuteWorkflowResponse
        {
            WorkflowInstanceId = source.WorkflowInstanceId,
            Status = workflowStatusMapper.Map(source.Status),
            SubStatus = workflowSubStatusMapper.Map(source.SubStatus),
            BookmarkDiff = bookmarkDiffMapper.Map(source.BookmarksDiff)
        };

        response.Incidents.AddRange(activityIncidentMapper.Map(source.Incidents));
        return response;
    }
}