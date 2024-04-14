using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.Distributed.Messages;

namespace Elsa.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="CreateWorkflowInstanceResponse"/> to <see cref="ProtoCreateWorkflowInstanceResponse"/> and vice versa.
/// </summary>
public class CreateWorkflowInstanceResponseMapper
{
    /// <summary>
    /// Maps <see cref="CreateWorkflowInstanceResponse"/> to <see cref="ProtoCreateWorkflowInstanceResponse"/>.
    /// </summary>
    public ProtoCreateWorkflowInstanceResponse Map(string workflowInstanceId, CreateWorkflowInstanceResponse? source)
    {
        if(source == null)
            return new();   
        
        return new();
    }
    
    /// <summary>
    /// Maps <see cref="ProtoCreateWorkflowInstanceResponse"/> to <see cref="CreateWorkflowInstanceResponse"/>.
    /// </summary>
    /// <param name="source"></param>
    public CreateWorkflowInstanceResponse Map(ProtoCreateWorkflowInstanceResponse source)
    {
        return new();
    }
}