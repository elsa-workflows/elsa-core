using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Serializes simple DTOs from and to JSON.
/// </summary>
public class JsonPayloadSerializer : IPayloadSerializer
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonPayloadSerializer"/> class.
    /// </summary>
    public JsonPayloadSerializer(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    /// <inheritdoc />
    public string Serialize(object payload)
    {
        var options = GetPayloadSerializerOptions();
        return JsonSerializer.Serialize(payload, options);
    }

    /// <inheritdoc />
    public object Deserialize(string payload)
    {
        return Deserialize<object>(payload);
    }
    
    /// <inheritdoc />
    public T Deserialize<T>(string payload)
    {
        var options = GetPayloadSerializerOptions();
        return JsonSerializer.Deserialize<T>(payload, options)!;
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
        options.Converters.Add(GetService<PolymorphicObjectConverterFactory>());
        options.Converters.Add(GetService<TypeJsonConverter>());
        options.Converters.Add(GetService<VariableConverterFactory>());
        return options;
    }
    
    private T GetService<T>() where T : notnull => ActivatorUtilities.GetServiceOrCreateInstance<T>(_serviceProvider);
}