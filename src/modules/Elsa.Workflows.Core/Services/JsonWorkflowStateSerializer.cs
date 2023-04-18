using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Core.Serialization.ReferenceHandlers;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Serializes and deserializes workflow states from and to JSON.
/// </summary>
public class JsonWorkflowStateSerializer : IWorkflowStateSerializer
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonWorkflowStateSerializer"/> class.
    /// </summary>
    public JsonWorkflowStateSerializer(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }

    /// <inheritdoc />
    public Task<string> SerializeAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var options = GetSerializerOptions();
        var json = JsonSerializer.Serialize(workflowState, options);
        return Task.FromResult(json);
    }

    /// <inheritdoc />
    public Task<WorkflowState> DeserializeAsync(string serializedState, CancellationToken cancellationToken = default)
    {
        var options = GetSerializerOptions();
        var workflowState = JsonSerializer.Deserialize<WorkflowState>(serializedState, options)!;
        return Task.FromResult(workflowState);
    }

    private JsonSerializerOptions GetSerializerOptions()
    {
        var referenceHandler = new CrossScopedReferenceHandler();

        var options = new JsonSerializerOptions
        {
            ReferenceHandler = referenceHandler,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new TypeJsonConverter(_wellKnownTypeRegistry));
        options.Converters.Add(JsonMetadataServices.TimeSpanConverter);
        options.Converters.Add(new PolymorphicObjectConverterFactory());
        return options;
    }
}