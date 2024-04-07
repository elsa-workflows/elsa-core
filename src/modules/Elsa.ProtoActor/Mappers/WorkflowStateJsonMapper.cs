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
    public async Task<WorkflowState> MapAsync(ProtoJson source, CancellationToken cancellationToken = default)
    {
        var workflowState = await workflowStateSerializer.DeserializeAsync(source.Text, cancellationToken);
        return workflowState;
    }

    /// <summary>
    /// Maps the specified <see cref="WorkflowState"/> to a <see cref="ProtoJson"/>.
    /// </summary>
    public async Task<ProtoJson> MapAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var json = await workflowStateSerializer.SerializeAsync(workflowState, cancellationToken);

        return new ProtoJson
        {
            Text = json
        };
    }
}