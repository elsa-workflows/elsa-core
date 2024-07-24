using Elsa.Common.Models;
using Elsa.Workflows.Runtime.ProtoActor.Extensions;
using Elsa.Workflows.Models;
using ProtoWorkflowDefinitionHandle = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.WorkflowDefinitionHandle;

namespace Elsa.Workflows.Runtime.ProtoActor.Mappers;

/// <summary>
/// Maps <see cref="WorkflowDefinitionHandle"/> to <see cref="ProtoWorkflowDefinitionHandle"/> and vice versa.
/// </summary>
public class WorkflowDefinitionHandleMapper
{
    /// <summary>
    /// Maps <see cref="WorkflowDefinitionHandle"/> to <see cref="ProtoWorkflowDefinitionHandle"/>.
    /// </summary>
    public ProtoWorkflowDefinitionHandle Map(WorkflowDefinitionHandle? source)
    {
        if(source == null)
            return new();   
        
        return new()
        {
            DefinitionId = source.DefinitionId.EmptyIfNull(),
            VersionOptions = source.VersionOptions == null ? "" : source.VersionOptions.ToString(),
            DefinitionVersionId = source.DefinitionVersionId.EmptyIfNull()
        };
    }
    
    /// <summary>
    /// Maps <see cref="ProtoWorkflowDefinitionHandle"/> to <see cref="WorkflowDefinitionHandle"/>.
    /// </summary>
    public WorkflowDefinitionHandle Map(ProtoWorkflowDefinitionHandle source)
    {
        return new()
        {
            DefinitionId = source.DefinitionId.NullIfEmpty(),
            VersionOptions = source.VersionOptions != "" ?  VersionOptions.FromString(source.VersionOptions) : null,
            DefinitionVersionId = source.DefinitionVersionId.NullIfEmpty()
        };
        
    }
}