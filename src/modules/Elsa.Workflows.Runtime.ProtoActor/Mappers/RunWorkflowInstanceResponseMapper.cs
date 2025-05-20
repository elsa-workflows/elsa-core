using Elsa.Extensions;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Messages;
using ProtoRunWorkflowInstanceResponse = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceResponse;
using Elsa.Workflows.Runtime.ProtoActor.Extensions;

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
    public ProtoRunWorkflowInstanceResponse Map(RunWorkflowResult source, bool includeOutput)
    {
        if(source.WorkflowState == null!)
            return new();

        var response = new ProtoRunWorkflowInstanceResponse
        {
            Status = workflowStatusMapper.Map(source.WorkflowState.Status),
            SubStatus = workflowSubStatusMapper.Map(source.WorkflowState.SubStatus)
        };

        response.Incidents.AddRange(activityIncidentMapper.Map(source.WorkflowState.Incidents));

        if (includeOutput)
            response.Output = source.WorkflowState.Output.SerializeOutput();
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
            Output = source.Output?.DeserializeOutput()
        };

        response.Incidents.AddRange(activityIncidentMapper.Map(source.Incidents));
        return response;

    }
}