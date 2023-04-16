using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class ApiSerializer : IApiSerializer
{
    private readonly IEnumerable<ISerializationOptionsConfigurator> _configurators;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiSerializer"/> class.
    /// </summary>
    public ApiSerializer(IServiceProvider serviceProvider, IEnumerable<ISerializationOptionsConfigurator> configurators)
    {
        _serviceProvider = serviceProvider;
        _configurators = configurators;
    }

    /// <inheritdoc />
    public string Serialize(object model)
    {
        var options = CreateOptions();
        return JsonSerializer.Serialize(model, options);
    }

    /// <inheritdoc />
    public object Deserialize(string serializedModel) => Deserialize<object>(serializedModel);

    /// <inheritdoc />
    public T Deserialize<T>(string serializedModel)
    {
        var options = CreateOptions();
        return JsonSerializer.Deserialize<T>(serializedModel, options)!;
    }

    /// <inheritdoc />
    public JsonSerializerOptions CreateOptions() => ApplyOptions(new JsonSerializerOptions());

    /// <inheritdoc />
    public JsonSerializerOptions ApplyOptions(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
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