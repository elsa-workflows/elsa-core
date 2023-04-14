using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Serializes and deserializes workflow states from and to JSON.
/// </summary>
public class JsonWorkflowStateSerializer : IWorkflowStateSerializer
{
    /// <inheritdoc />
    public async Task<string> SerializeAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var options = GetSerializerOptions();
        var json = JsonSerializer.Serialize(workflowState, options);
        return json;
    }

    /// <inheritdoc />
    public async Task<WorkflowState> DeserializeAsync(string serializedState, CancellationToken cancellationToken = default)
    {
        var options = GetSerializerOptions();
        var workflowState = JsonSerializer.Deserialize<WorkflowState>(serializedState, options)!;
        return workflowState;
    }

    private JsonSerializerOptions GetSerializerOptions()
    {
        var options = new JsonSerializerOptions();

        options.Converters.Add(new JsonStringEnumConverter());
        // options.Converters.Add(new PolymorphicObjectConverterFactory());
        // options.Converters.Add(new PolymorphicDictionaryConverterFactory());
        return options;
    }
}