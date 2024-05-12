using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Common.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.ReferenceHandlers;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Serialization.Serializers;

/// <summary>
/// Serializes and deserializes workflow states from and to JSON.
/// </summary>
public class JsonWorkflowStateSerializer : ConfigurableSerializer, IWorkflowStateSerializer
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonWorkflowStateSerializer"/> class.
    /// </summary>
    public JsonWorkflowStateSerializer(IServiceProvider serviceProvider, IWellKnownTypeRegistry wellKnownTypeRegistry, ILoggerFactory loggerFactory) : base(serviceProvider)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    public string Serialize(WorkflowState workflowState)
    {
        var options = GetOptions();
        return JsonSerializer.Serialize(workflowState, options);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    public byte[] SerializeToUtfBytes(WorkflowState workflowState)
    {
        var options = GetOptions();
        return JsonSerializer.SerializeToUtf8Bytes(workflowState, options);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    public JsonElement SerializeToElement(WorkflowState workflowState)
    {
        var options = GetOptions();
        return JsonSerializer.SerializeToElement(workflowState, options);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    public string Serialize(object workflowState)
    {
        var options = GetOptions();
        return JsonSerializer.Serialize(workflowState, workflowState.GetType(), options);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    public WorkflowState Deserialize(string serializedState)
    {
        var options = GetOptions();
        return JsonSerializer.Deserialize<WorkflowState>(serializedState, options)!;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    public WorkflowState Deserialize(JsonElement serializedState)
    {
        var options = GetOptions();
        return serializedState.Deserialize<WorkflowState>(options)!;
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    public T Deserialize<T>(string serializedState)
    {
        var options = GetOptions();
        return JsonSerializer.Deserialize<T>(serializedState, options)!;
    }

    /// <inheritdoc />
    public override JsonSerializerOptions GetOptions()
    {
        // Bypass cached options to ensure that the reference handler is always fresh.
        return GetOptionsInternal();
    }

    /// <inheritdoc />
    protected override void Configure(JsonSerializerOptions options)
    {
        var referenceHandler = new CrossScopedReferenceHandler();

        options.ReferenceHandler = referenceHandler;
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }

    /// <inheritdoc />
    protected override void AddConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new TypeJsonConverter(_wellKnownTypeRegistry));
        options.Converters.Add(JsonMetadataServices.TimeSpanConverter);
        options.Converters.Add(new PolymorphicObjectConverterFactory());
        options.Converters.Add(new TypeJsonConverter(_wellKnownTypeRegistry));
        options.Converters.Add(new VariableConverterFactory(_wellKnownTypeRegistry, _loggerFactory));
    }
}