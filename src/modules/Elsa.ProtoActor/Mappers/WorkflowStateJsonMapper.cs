using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.State;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps between <see cref="ProtoJson"/> and <see cref="WorkflowState"/>.
/// </summary>
/// <param name="workflowStateSerializer"></param>
public class WorkflowStateJsonMapper(IWorkflowStateSerializer workflowStateSerializer)
{
    /// <summary>
    /// Maps the specified <see cref="ProtoJson"/> to a <see cref="WorkflowState"/>.
    /// </summary>
    public WorkflowState Map(ProtoJson source)
    {
        return workflowStateSerializer.Deserialize(source.Text);
    }

    /// <summary>
    /// Maps the specified <see cref="WorkflowState"/> to a <see cref="ProtoJson"/>.
    /// </summary>
    public ProtoJson Map(WorkflowState workflowState)
    {
        var json = workflowStateSerializer.Serialize(workflowState);

        return new ProtoJson
        {
            Text = json
        };
    }
}