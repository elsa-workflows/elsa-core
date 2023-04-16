using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Serializes simple DTOs from and to JSON.
/// </summary>
public class JsonPayloadSerializer : IPayloadSerializer
{
    /// <inheritdoc />
    public Task<string> SerializeAsync(object payload, CancellationToken cancellationToken = default)
    {
        var options = GetPayloadSerializerOptions();
        var json = JsonSerializer.Serialize(payload, options);
        return Task.FromResult(json);
    }

    /// <inheritdoc />
    public Task<object> DeserializeAsync(string payload, CancellationToken cancellationToken = default)
    {
        return DeserializeAsync<object>(payload, cancellationToken);
    }
    
    /// <inheritdoc />
    public Task<T> DeserializeAsync<T>(string payload, CancellationToken cancellationToken = default)
    {
        var options = GetPayloadSerializerOptions();
        var workflowState = JsonSerializer.Deserialize<T>(payload, options)!;
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
        options.Converters.Add(new PolymorphicObjectConverterFactory());
        return options;
    }
}