using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.Distributed.Messages;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="CreateWorkflowInstanceRequest"/> to <see cref="ProtoCreateWorkflowInstanceRequest"/> and vice versa.
/// </summary>
public class CreateWorkflowInstanceRequestMapper
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
            WorkflowDefinitionVersionId = source.WorkflowDefinitionVersionId,
            WorkflowInstanceId = workflowInstanceId,
            CorrelationId = source.CorrelationId,
            ParentId = source.ParentId,
            Input = source.Input?.SerializeInput(),
            Properties = source.Properties?.SerializeProperties()
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
            WorkflowDefinitionVersionId = source.WorkflowDefinitionVersionId,
            CorrelationId = source.CorrelationId,
            ParentId = source.ParentId,
            Input = source.Input?.DeserializeInput(),
            Properties = source.Properties?.DeserializeProperties()
        };
    }
}