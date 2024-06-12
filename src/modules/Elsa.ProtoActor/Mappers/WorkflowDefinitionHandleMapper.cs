using Elsa.Common.Models;
using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Models;

namespace Elsa.ProtoActor.Mappers;

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