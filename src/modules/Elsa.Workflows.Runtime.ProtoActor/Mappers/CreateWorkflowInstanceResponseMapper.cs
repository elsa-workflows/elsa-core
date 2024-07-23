using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.Runtime.Messages;

namespace Elsa.Workflows.Runtime.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="CreateWorkflowInstanceResponse"/> to <see cref="ProtoCreateWorkflowInstanceResponse"/> and vice versa.
/// </summary>
public class CreateWorkflowInstanceResponseMapper
{
    /// Maps <see cref="CreateWorkflowInstanceResponse"/> to <see cref="ProtoCreateWorkflowInstanceResponse"/>.
    public ProtoCreateWorkflowInstanceResponse Map(CreateWorkflowInstanceResponse? source)
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