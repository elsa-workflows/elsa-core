using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="IExecuteWorkflowRequest"/> to <see cref="ProtoExecuteWorkflowRequest"/>.
/// </summary>
public class ExecuteWorkflowRequestMapper(ActivityHandleMapper activityHandleMapper)
{
    /// <summary>
    /// Maps <see cref="IExecuteWorkflowRequest"/> to <see cref="ProtoExecuteWorkflowRequest"/>.
    /// </summary>
    public ProtoExecuteWorkflowRequest Map(string workflowDefinitionVersionId, IExecuteWorkflowRequest? source)
    {
        return new()
        {
            WorkflowDefinitionVersionId = workflowDefinitionVersionId,
            ActivityHandle = activityHandleMapper.Map(source?.ActivityHandle),
            BookmarkId = source?.BookmarkId,
            ParentWorkflowInstanceId = source?.ParentWorkflowInstanceId,
            TriggerActivityId = source?.TriggerActivityId,
            Input = source?.Input?.SerializeInput(),
            CorrelationId = source?.CorrelationId,
            Properties = source?.Properties?.SerializeProperties(),
        };
    }
}