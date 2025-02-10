using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Expressions.Contracts;
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
    [Obsolete("Use the non-async version Serialize instead.")]
    public Task<string> SerializeAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Serialize(workflowState));
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    [Obsolete("Use the non-async version SerializeToUtfBytes instead.")]
    public Task<byte[]> SerializeToUtfBytesAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SerializeToUtfBytes(workflowState));
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    [Obsolete("Use the non-async version SerializeToElement instead.")]
    public Task<JsonElement> SerializeToElementAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SerializeToElement(workflowState));
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The serialization process may require access to the type.")]
    [Obsolete("Use the non-async version Serialize instead.")]
    public Task<string> SerializeAsync(object workflowState, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Serialize(workflowState));
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    [Obsolete("Use the non-async version Deserialize instead.")]
    public Task<WorkflowState> DeserializeAsync(string serializedState, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Deserialize(serializedState));
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    [Obsolete("Use the non-async version Deserialize instead.")]
    public Task<WorkflowState> DeserializeAsync(JsonElement serializedState, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Deserialize(serializedState));
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("The type 'T' may be trimmed from the output. The deserialization process may require access to the type.")]
    [Obsolete("Use the non-async version Deserialize instead.")]
    public Task<T> DeserializeAsync<T>(string serializedState, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Deserialize<T>(serializedState));
    }

    public string Serialize(WorkflowState workflowState)
    {
        var options = GetOptions();
        return JsonSerializer.Serialize(workflowState, options);
    }

    public byte[] SerializeToUtfBytes(WorkflowState workflowState)
    {
        var options = GetOptions();
        return JsonSerializer.SerializeToUtf8Bytes(workflowState, options);
    }

    public JsonElement SerializeToElement(WorkflowState workflowState)
    {
        var options = GetOptions();
        return JsonSerializer.SerializeToElement(workflowState, options);
    }

    public string Serialize(object workflowState)
    {
        var options = GetOptions();
        return JsonSerializer.Serialize(workflowState, workflowState.GetType(), options);
    }

    public WorkflowState Deserialize(string serializedState)
    {
        var options = GetOptions();
        return JsonSerializer.Deserialize<WorkflowState>(serializedState, options)!;
    }

    public WorkflowState Deserialize(JsonElement serializedState)
    {
        var options = GetOptions();
        return serializedState.Deserialize<WorkflowState>(options)!;
    }

    public T Deserialize<T>(string serializedState)
    {
        var options = GetOptions();
        return JsonSerializer.Deserialize<T>(serializedState, options)!;
    }

    /// <inheritdoc />
    public override JsonSerializerOptions GetOptions()
    {
        var options = base.GetOptions();
        return new JsonSerializerOptions(options)
        {
            ReferenceHandler = new CrossScopedReferenceHandler()
        };
    }

    /// <inheritdoc />
    protected override void AddConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new TypeJsonConverter(_wellKnownTypeRegistry));
        options.Converters.Add(new PolymorphicObjectConverterFactory(_wellKnownTypeRegistry));
        options.Converters.Add(new VariableConverterFactory(_wellKnownTypeRegistry, _loggerFactory));
    }
}