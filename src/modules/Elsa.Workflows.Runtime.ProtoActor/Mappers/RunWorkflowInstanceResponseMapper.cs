using Elsa.Extensions;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Messages;
using ProtoRunWorkflowInstanceResponse = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceResponse;

namespace Elsa.Workflows.Runtime.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="RunWorkflowResult"/> to <see cref="ProtoRunWorkflowInstanceResponse"/> and vice versa.
/// </summary>
public class RunWorkflowInstanceResponseMapper(
    WorkflowStatusMapper workflowStatusMapper,
    WorkflowSubStatusMapper workflowSubStatusMapper,
    ActivityIncidentMapper activityIncidentMapper)
{
    /// <summary>
    /// Maps <see cref="RunWorkflowResult"/> to <see cref="ProtoRunWorkflowInstanceResponse"/>.
    /// </summary>
    public ProtoRunWorkflowInstanceResponse Map(RunWorkflowResult source)
    {
        if(source.WorkflowState == null!)
            return new ProtoRunWorkflowInstanceResponse();
        
        var response = new ProtoRunWorkflowInstanceResponse
        {
            Status = workflowStatusMapper.Map(source.WorkflowState.Status),
            SubStatus = workflowSubStatusMapper.Map(source.WorkflowState.SubStatus)
        };

        response.Incidents.AddRange(activityIncidentMapper.Map(source.WorkflowState.Incidents));
        return response;
    }

    /// <summary>
    /// Maps <see cref="ProtoRunWorkflowInstanceResponse"/> to <see cref="RunWorkflowInstanceResponse"/>.
    /// </summary>
    public RunWorkflowInstanceResponse Map(string workflowInstanceId, ProtoRunWorkflowInstanceResponse source)
    {
        var response = new RunWorkflowInstanceResponse
        {
            WorkflowInstanceId = workflowInstanceId,
            Status = workflowStatusMapper.Map(source.Status),
            SubStatus = workflowSubStatusMapper.Map(source.SubStatus),
        };

        response.Incidents.AddRange(activityIncidentMapper.Map(source.Incidents));
        return response;
        
    }
}