using Elsa.Workflows.Core.State;
using ProtoWorkflowFault = Elsa.ProtoActor.Protos.WorkflowFault;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps between <see cref="WorkflowFaultState"/> and <see cref="ProtoWorkflowFault"/>.
/// </summary>
public class WorkflowFaultStateMapper
{
    private readonly ExceptionMapper _exceptionMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowFaultStateMapper"/> class.
    /// </summary>
    public WorkflowFaultStateMapper(ExceptionMapper exceptionMapper)
    {
        _exceptionMapper = exceptionMapper;
    }

    /// <summary>
    /// Maps a <see cref="ProtoWorkflowFault"/> to a <see cref="WorkflowFaultState"/>.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>The mapped <see cref="WorkflowFaultState"/>.</returns>
    public WorkflowFaultState Map(ProtoWorkflowFault source) =>
        new(_exceptionMapper.Map(source.Exception), source.Message, source.FaultedActivityId);

    /// <summary>
    /// Maps a <see cref="WorkflowFaultState"/> to a <see cref="ProtoWorkflowFault"/>.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <returns>The mapped <see cref="ProtoWorkflowFault"/>.</returns>
    public ProtoWorkflowFault Map(WorkflowFaultState source) =>
        new()
        {
            Exception = source.Exception != null ? _exceptionMapper.Map(source.Exception) : default,
            Message = source.Message,
            FaultedActivityId = source.FaultedActivityId
        };
}