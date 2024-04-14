using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="IExecuteWorkflowRequest"/> to <see cref="ProtoExecuteWorkflowRequest"/>.
/// </summary>
public class ExecuteWorkflowRequestMapper(ActivityHandleMapper activityHandleMapper)
{
    /// <summary>
    /// Maps <see cref="IExecuteWorkflowRequest"/> to <see cref="ProtoExecuteWorkflowRequest"/>.
    /// </summary>
    public ProtoExecuteWorkflowRequest Map(string workflowDefinitionVersionId, string workflowInstanceId, IExecuteWorkflowRequest? source)
    {
        return new()
        {
            WorkflowDefinitionVersionId = workflowDefinitionVersionId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityHandle = activityHandleMapper.Map(source?.ActivityHandle) ?? new(),
            BookmarkId = source?.BookmarkId.EmptyIfNull(),
            ParentWorkflowInstanceId = source?.ParentWorkflowInstanceId.EmptyIfNull(),
            TriggerActivityId = source?.TriggerActivityId.EmptyIfNull(),
            Input = source?.Input?.SerializeInput(),
            CorrelationId = source?.CorrelationId.EmptyIfNull(),
            Properties = source?.Properties?.SerializeProperties(),
            IsNewInstance = source?.IsExistingInstance ?? false,
        };
    }
    
    /// <summary>
    /// Maps <see cref="ProtoExecuteWorkflowRequest"/> to <see cref="IExecuteWorkflowRequest"/>.
    /// </summary>
    public IExecuteWorkflowRequest Map(ProtoExecuteWorkflowRequest source)
    {
        return new ExecuteWorkflowRequest
        {
            ActivityHandle = activityHandleMapper.Map(source.ActivityHandle),
            BookmarkId = source.BookmarkId,
            ParentWorkflowInstanceId = source.ParentWorkflowInstanceId,
            TriggerActivityId = source.TriggerActivityId,
            Input = source.Input?.DeserializeInput(),
            CorrelationId = source.CorrelationId,
            Properties = source.Properties?.DeserializeProperties(),
            IsExistingInstance = source.IsNewInstance,
        };
    }
}