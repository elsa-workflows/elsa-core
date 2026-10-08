using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.Management;

/// <summary>The Management store's shadow-state codec, also used by the selected admission bootstrap.</summary>
public static class WorkflowDefinitionStateCodec
{
    public static string Serialize(WorkflowDefinition entity, IPayloadSerializer serializer) =>
        serializer.Serialize(new EFCoreWorkflowDefinitionStore.WorkflowDefinitionState(entity.Options, entity.Variables, entity.Inputs, entity.Outputs, entity.Outcomes, entity.CustomProperties));

    public static void Write(ManagementElsaDbContext context, WorkflowDefinition entity, IPayloadSerializer serializer)
    {
        context.Entry(entity).Property("Data").CurrentValue = Serialize(entity, serializer);
        context.Entry(entity).Property("UsableAsActivity").CurrentValue = entity.Options.UsableAsActivity;
    }

    /// <summary>Strict decoding. Bootstrap must reject corrupt state rather than accept a fallback.</summary>
    public static void Read(ManagementElsaDbContext context, WorkflowDefinition entity, IPayloadSerializer serializer, bool requireStoredState = false)
    {
        var json = (string?)context.Entry(entity).Property("Data").CurrentValue;
        if (string.IsNullOrWhiteSpace(json))
        {
            if (requireStoredState)
            {
                throw new InvalidOperationException("workflow_definition_stored_state_missing");
            }
            return;
        }
        var data = serializer.Deserialize<EFCoreWorkflowDefinitionStore.WorkflowDefinitionState>(json) ?? throw new InvalidOperationException("workflow_definition_stored_state_missing");
        entity.Options = data.Options;
        entity.Variables = data.Variables;
        entity.Inputs = data.Inputs;
        entity.Outputs = data.Outputs;
        entity.Outcomes = data.Outcomes;
        entity.CustomProperties = data.CustomProperties;
    }

}
