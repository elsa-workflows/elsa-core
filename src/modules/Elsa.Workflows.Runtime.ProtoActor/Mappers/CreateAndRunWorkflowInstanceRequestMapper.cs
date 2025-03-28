using Elsa.Extensions;
using Elsa.Workflows.Runtime.ProtoActor.Extensions;
using Elsa.Workflows.Runtime.Messages;
using ProtoCreateAndRunWorkflowInstanceRequest = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.CreateAndRunWorkflowInstanceRequest;
using ProtoInput = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.Input;
using ProtoProperties = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.Properties;
namespace Elsa.Workflows.Runtime.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="CreateAndRunWorkflowInstanceRequestMapper"/> to <see cref="ProtoCreateAndRunWorkflowInstanceRequest"/> and vice versa.
/// </summary>
public class CreateAndRunWorkflowInstanceRequestMapper(WorkflowDefinitionHandleMapper workflowDefinitionHandleMapper, ActivityHandleMapper activityHandleMapper)
{
    /// <summary>
    /// Maps <see cref="CreateAndRunWorkflowInstanceRequestMapper"/> to <see cref="ProtoCreateAndRunWorkflowInstanceRequest"/>.
    /// </summary>
    public ProtoCreateAndRunWorkflowInstanceRequest Map(string workflowInstanceId, CreateAndRunWorkflowInstanceRequest? source)
    {
        if (source == null)
            return new();

        return new()
        {
            WorkflowDefinitionHandle = workflowDefinitionHandleMapper.Map(source.WorkflowDefinitionHandle),
            WorkflowInstanceId = workflowInstanceId.EmptyIfNull(),
            CorrelationId = source.CorrelationId.EmptyIfNull(),
            Name = source.Name.EmptyIfNull(),
            ParentId = source.ParentId.EmptyIfNull(),
            Input = source.Input?.SerializeInput() ?? new ProtoInput(),
            Properties = source.Properties?.SerializeProperties() ?? new ProtoProperties(),
            TriggerActivityId = source.TriggerActivityId.EmptyIfNull(),
            ActivityHandle = activityHandleMapper.Map(source?.ActivityHandle) ?? new(),
        };
    }

    /// <summary>
    /// Maps <see cref="ProtoCreateAndRunWorkflowInstanceRequest"/> to <see cref="CreateAndRunWorkflowInstanceRequestMapper"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public CreateAndRunWorkflowInstanceRequest Map(ProtoCreateAndRunWorkflowInstanceRequest source)
    {
        return new()
        {
            WorkflowDefinitionHandle = workflowDefinitionHandleMapper.Map(source.WorkflowDefinitionHandle),
            CorrelationId = source.CorrelationId.NullIfEmpty(),
            Name = source.Name.NullIfEmpty(),
            ParentId = source.ParentId.NullIfEmpty(),
            Input = source.Input?.DeserializeInput(),
            Properties = source.Properties?.DeserializeProperties(),
            ActivityHandle = activityHandleMapper.Map(source.ActivityHandle),
        };
    }
}