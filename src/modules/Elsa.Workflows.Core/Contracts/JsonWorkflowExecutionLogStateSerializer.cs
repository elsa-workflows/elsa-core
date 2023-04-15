using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Serializes and execution log record payloads from and to JSON.
/// </summary>
public class JsonWorkflowExecutionLogStateSerializer : IWorkflowExecutionLogStateSerializer
{
    /// <inheritdoc />
    public Task<string> SerializeAsync(object payload, CancellationToken cancellationToken = default)
    {
        var options = GetPayloadSerializerOptions();
        var json = JsonSerializer.Serialize(payload, options);
        return Task.FromResult(json);
    }

    /// <inheritdoc />
    public Task<string> SerializeAsync(IDictionary<string, object> payload, CancellationToken cancellationToken = default)
    {
        var options = GetDictionarySerializerOptions();
        var json = JsonSerializer.Serialize(payload, options);
        return Task.FromResult(json);
    }

    /// <inheritdoc />
    public Task<object> DeserializeAsync(string payload, CancellationToken cancellationToken = default)
    {
        var options = GetPayloadSerializerOptions();
        var workflowState = JsonSerializer.Deserialize<object>(payload, options)!;
        return Task.FromResult(workflowState);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object>> DeserializeDictionaryAsync(string payload, CancellationToken cancellationToken = default)
    {
        var options = GetDictionarySerializerOptions();
        var workflowState = JsonSerializer.Deserialize<IDictionary<string, object>>(payload, options)!;
        return Task.FromResult(workflowState);
    }
    
    private JsonSerializerOptions GetPayloadSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(JsonMetadataServices.TimeSpanConverter);
        return options;
    }
    
    private JsonSerializerOptions GetDictionarySerializerOptions()
    {
        var options = GetPayloadSerializerOptions();
        return options;
    }
}