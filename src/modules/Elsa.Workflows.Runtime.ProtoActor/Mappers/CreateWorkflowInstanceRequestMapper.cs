using Elsa.Workflows.Runtime.ProtoActor.Extensions;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.Messages;

namespace Elsa.Workflows.Runtime.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="CreateWorkflowInstanceRequest"/> to <see cref="ProtoCreateWorkflowInstanceRequest"/> and vice versa.
/// </summary>
public class CreateWorkflowInstanceRequestMapper(WorkflowDefinitionHandleMapper workflowDefinitionHandleMapper)
{
    /// <summary>
    /// Maps <see cref="CreateWorkflowInstanceRequest"/> to <see cref="ProtoCreateWorkflowInstanceRequest"/>.
    /// </summary>
    public ProtoCreateWorkflowInstanceRequest Map(string workflowInstanceId, CreateWorkflowInstanceRequest? source)
    {
        if(source == null)
            return new();   
        
        return new()
        {
            WorkflowDefinitionHandle = workflowDefinitionHandleMapper.Map(source.WorkflowDefinitionHandle),
            WorkflowInstanceId = workflowInstanceId,
            CorrelationId = source.CorrelationId.EmptyIfNull(),
            ParentId = source.ParentId.EmptyIfNull(),
            Input = source.Input?.SerializeInput() ?? new ProtoInput(),
            Properties = source.Properties?.SerializeProperties() ?? new ProtoProperties()
        };
    }
    
    /// <summary>
    /// Maps <see cref="ProtoCreateWorkflowInstanceRequest"/> to <see cref="CreateWorkflowInstanceRequest"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    public CreateWorkflowInstanceRequest Map(ProtoCreateWorkflowInstanceRequest source)
    {
        return new()
        {
            WorkflowDefinitionHandle = workflowDefinitionHandleMapper.Map(source.WorkflowDefinitionHandle),
            CorrelationId = source.CorrelationId,
            ParentId = source.ParentId,
            Input = source.Input?.DeserializeInput(),
            Properties = source.Properties?.DeserializeProperties()
        };
    }
}