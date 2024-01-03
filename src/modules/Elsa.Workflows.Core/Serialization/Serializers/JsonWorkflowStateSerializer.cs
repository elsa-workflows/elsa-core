using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Expressions.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Serialization.Converters;
using Elsa.Workflows.Serialization.ReferenceHandlers;
using Elsa.Workflows.State;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Serialization.Serializers;

/// <summary>
/// Serializes and deserializes workflow states from and to JSON.
/// </summary>
public class JsonWorkflowStateSerializer : IWorkflowStateSerializer
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;
    private readonly INotificationSender _notificationSender;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonWorkflowStateSerializer"/> class.
    /// </summary>
    public JsonWorkflowStateSerializer(IWellKnownTypeRegistry wellKnownTypeRegistry, INotificationSender notificationSender, ILoggerFactory loggerFactory)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
        _notificationSender = notificationSender;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public async Task<string> SerializeAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var options = GetSerializerOptions();
        var serializingWorkflowState = new SerializingWorkflowState(options);
        await _notificationSender.SendAsync(serializingWorkflowState, cancellationToken);

        return JsonSerializer.Serialize(workflowState, options);
    }

    /// <inheritdoc />
    public async Task<byte[]> SerializeToUtfBytesAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var options = GetSerializerOptions();
        var serializingWorkflowState = new SerializingWorkflowState(options);
        await _notificationSender.SendAsync(serializingWorkflowState, cancellationToken);

        return JsonSerializer.SerializeToUtf8Bytes(workflowState, options);
    }

    /// <inheritdoc />
    public async Task<JsonElement> SerializeToElementAsync(WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var options = GetSerializerOptions();
        var serializingWorkflowState = new SerializingWorkflowState(options);
        await _notificationSender.SendAsync(serializingWorkflowState, cancellationToken);

        return JsonSerializer.SerializeToElement(workflowState, options);
    }

    /// <inheritdoc />
    public Task<string> SerializeAsync(object workflowState, CancellationToken cancellationToken = default)
    {
        var options = GetSerializerOptions();
        var json = JsonSerializer.Serialize(workflowState, workflowState.GetType(), options);
        return Task.FromResult(json);
    }

    /// <inheritdoc />
    public Task<WorkflowState> DeserializeAsync(string serializedState, CancellationToken cancellationToken = default)
    {
        var options = GetSerializerOptions();
        var workflowState = JsonSerializer.Deserialize<WorkflowState>(serializedState, options)!;
        return Task.FromResult(workflowState);
    }

    /// <inheritdoc />
    public Task<WorkflowState> DeserializeAsync(JsonElement serializedState, CancellationToken cancellationToken = default)
    {
        var options = GetSerializerOptions();
        var workflowState = serializedState.Deserialize<WorkflowState>(options)!;
        return Task.FromResult(workflowState);
    }

    /// <inheritdoc />
    public Task<T> DeserializeAsync<T>(string serializedState, CancellationToken cancellationToken = default)
    {
        var options = GetSerializerOptions();
        var workflowState = JsonSerializer.Deserialize<T>(serializedState, options)!;
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
        options.Converters.Add(new TypeJsonConverter(_wellKnownTypeRegistry));
        options.Converters.Add(new VariableConverterFactory(_wellKnownTypeRegistry, _loggerFactory));

        return options;
    }
}