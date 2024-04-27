using Elsa.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Results;

namespace Elsa.ProtoActor.Mappers;

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
        var response = new ProtoRunWorkflowInstanceResponse
        {
            Status = workflowStatusMapper.Map(source.Status),
            SubStatus = workflowSubStatusMapper.Map(source.SubStatus),
        };

        response.Incidents.AddRange(activityIncidentMapper.Map(source.Incidents));
        return response;
    }

    /// <summary>
    /// Maps <see cref="ProtoRunWorkflowInstanceResponse"/> to <see cref="RunWorkflowInstanceResponse"/>.
    /// </summary>
    public RunWorkflowInstanceResponse Map(ProtoRunWorkflowInstanceResponse source)
    {
        var response = new RunWorkflowInstanceResponse
        {
            Status = workflowStatusMapper.Map(source.Status),
            SubStatus = workflowSubStatusMapper.Map(source.SubStatus),
        };

        response.Incidents.AddRange(activityIncidentMapper.Map(source.Incidents));
        return response;
        
    }
}