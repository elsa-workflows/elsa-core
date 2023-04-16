using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Serializes and deserializes activities from and to JSON.
/// </summary>
public class JsonActivitySerializer : IActivitySerializer
{
    private readonly IEnumerable<ISerializationOptionsConfigurator> _configurators;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonActivitySerializer"/> class.
    /// </summary>
    public JsonActivitySerializer(IEnumerable<ISerializationOptionsConfigurator> configurators, IServiceProvider serviceProvider)
    {
        _configurators = configurators;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public string Serialize(IActivity activity)
    {
        var options = CreateOptions();
        options.Converters.Add(Create<JsonIgnoreCompositeRootConverterFactory>());
        return JsonSerializer.Serialize(activity, activity.GetType(), options);
    }

    /// <inheritdoc />
    public string Serialize(object value)
    {
        var options = CreateOptions();
        options.Converters.Add(Create<JsonIgnoreCompositeRootConverterFactory>());
        return JsonSerializer.Serialize(value, options);
    }

    /// <inheritdoc />
    public IActivity Deserialize(string serializedActivity) => JsonSerializer.Deserialize<IActivity>(serializedActivity, CreateOptions())!;

    /// <inheritdoc />
    public object Deserialize(string serializedValue, Type type) => JsonSerializer.Deserialize(serializedValue, type, CreateOptions())!;

    /// <inheritdoc />
    public T Deserialize<T>(string serializedValue) => JsonSerializer.Deserialize<T>(serializedValue, CreateOptions())!;

    private JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(Create<JsonStringEnumConverter>());
        options.Converters.Add(Create<TypeJsonConverter>());
        options.Converters.Add(JsonMetadataServices.TimeSpanConverter);

        // Give external packages a chance to further configure the serializer options.
        foreach (var configurator in _configurators)
            configurator.Configure(options);

        return options;
    }

    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}