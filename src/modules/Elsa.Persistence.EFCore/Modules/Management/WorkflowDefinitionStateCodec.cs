using System.Text.Json.Serialization;
using Elsa.Workflows;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.Modules.Management;

/// <summary>The Management store's shadow-state codec, also used by the selected admission bootstrap.</summary>
public static class WorkflowDefinitionStateCodec
{
    public static string Serialize(WorkflowDefinition entity, IPayloadSerializer serializer) =>
        serializer.Serialize(new WorkflowDefinitionState(entity.Options, entity.Variables, entity.Inputs, entity.Outputs, entity.Outcomes, entity.CustomProperties));

    public static void Write(ManagementElsaDbContext context, WorkflowDefinition entity, IPayloadSerializer serializer)
    {
        context.Entry(entity).Property("Data").CurrentValue = Serialize(entity, serializer);
        context.Entry(entity).Property("UsableAsActivity").CurrentValue = entity.Options.UsableAsActivity;
    }

    /// <summary>Strict decoding. Bootstrap must reject corrupt state rather than accept a fallback.</summary>
    public static void Read(ManagementElsaDbContext context, WorkflowDefinition entity, IPayloadSerializer serializer)
    {
        var json = (string?)context.Entry(entity).Property("Data").CurrentValue;
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }
        var data = serializer.Deserialize<WorkflowDefinitionState>(json);
        entity.Options = data.Options;
        entity.Variables = data.Variables;
        entity.Inputs = data.Inputs;
        entity.Outputs = data.Outputs;
        entity.Outcomes = data.Outcomes;
        entity.CustomProperties = data.CustomProperties;
    }

    private sealed class WorkflowDefinitionState
    {
        [JsonConstructor]
        public WorkflowDefinitionState()
        {
        }

        public WorkflowDefinitionState(
            WorkflowOptions options,
            ICollection<Variable> variables,
            ICollection<InputDefinition> inputs,
            ICollection<OutputDefinition> outputs,
            ICollection<string> outcomes,
            IDictionary<string, object> customProperties
        )
        {
            Options = options;
            Variables = variables;
            Inputs = inputs;
            Outputs = outputs;
            Outcomes = outcomes;
            CustomProperties = customProperties;
        }

        public WorkflowOptions Options { get; set; } = new();
        public ICollection<Variable> Variables { get; set; } = new List<Variable>();
        public ICollection<InputDefinition> Inputs { get; set; } = new List<InputDefinition>();
        public ICollection<OutputDefinition> Outputs { get; set; } = new List<OutputDefinition>();
        public ICollection<string> Outcomes { get; set; } = new List<string>();
        public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
    }
}
