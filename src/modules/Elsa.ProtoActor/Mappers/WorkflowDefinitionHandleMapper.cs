using Elsa.Common.Models;
using Elsa.ProtoActor.Extensions;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Management;

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
            DefinitionId = source.DefinitionId.NullIfEmpty(),
            VersionOptions = source.VersionOptions?.ToString().NullIfEmpty(),
            DefinitionVersionId = source.DefinitionVersionId.NullIfEmpty()
        };
    }
    
    /// <summary>
    /// Maps <see cref="ProtoWorkflowDefinitionHandle"/> to <see cref="WorkflowDefinitionHandle"/>.
    /// </summary>
    public WorkflowDefinitionHandle Map(ProtoWorkflowDefinitionHandle source)
    {
        return new()
        {
            DefinitionId = source.DefinitionId.EmptyIfNull(),
            VersionOptions = source.VersionOptions != "" ?  VersionOptions.FromString(source.VersionOptions) : null,
            DefinitionVersionId = source.DefinitionVersionId.EmptyIfNull()
        };
        
    }
}