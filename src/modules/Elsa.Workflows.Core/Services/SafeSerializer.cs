using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class SafeSerializer : ISafeSerializer
{
    private readonly IEnumerable<ISafeSerializerConfigurator> _configurators;

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeSerializer"/> class.
    /// </summary>
    public SafeSerializer(IEnumerable<ISafeSerializerConfigurator> configurators)
    {
        _configurators = configurators;
    }
    
    /// <inheritdoc />
    public string Serialize(object? value)
    {
        var options = CreateOptions();
        return JsonSerializer.Serialize(value, options);
    }

    /// <inheritdoc />
    public T Deserialize<T>(string json)
    {
        var options = CreateOptions();
        return JsonSerializer.Deserialize<T>(json, options)!;
    }
    
    private JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new TypeJsonConverter(WellKnownTypeRegistry.CreateDefault()),
                new SafeValueConverterFactory()
            }
        };

        foreach (var configurator in _configurators) 
            configurator.ConfigureOptions(options);

        return options;
    }
}