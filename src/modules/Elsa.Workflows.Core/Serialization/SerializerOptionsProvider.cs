using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Serialization;

public class SerializerOptionsProvider
{
    private readonly IEnumerable<ISerializationOptionsConfigurator> _configurators;
    private readonly IServiceProvider _serviceProvider;

    public SerializerOptionsProvider(IEnumerable<ISerializationOptionsConfigurator> configurators, IServiceProvider serviceProvider)
    {
        _configurators = configurators;
        _serviceProvider = serviceProvider;
    }

    public JsonSerializerOptions CreateApiOptions(ReferenceHandler? referenceHandler = default) => CreateDefaultOptions(referenceHandler ?? ReferenceHandler.IgnoreCycles);

    public JsonSerializerOptions CreatePersistenceOptions(ReferenceHandler? referenceHandler = default)
    {
        var options = CreateDefaultOptions(referenceHandler ?? ReferenceHandler.IgnoreCycles);
        options.Converters.Add(Create<JsonIgnoreCompositeRootConverterFactory<IActivity>>());
        return options;
    }

    public JsonSerializerOptions CreateDefaultOptions(ReferenceHandler? referenceHandling = default)
    {
        var options = new JsonSerializerOptions();
        ApplyDefaultOptions(options, referenceHandling);
        return options;
    }

    public void ApplyApiOptions(JsonSerializerOptions options, ReferenceHandler? referenceHandling = default) => ApplyDefaultOptions(options, referenceHandling ?? ReferenceHandler.IgnoreCycles);

    public void ApplyPersistenceOptions(JsonSerializerOptions options, ReferenceHandler? referenceHandling = default) => ApplyDefaultOptions(options, referenceHandling ?? ReferenceHandler.IgnoreCycles);

    public void ApplyDefaultOptions(JsonSerializerOptions options, ReferenceHandler? referenceHandling = default)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.ReferenceHandler = referenceHandling ?? ReferenceHandler.Preserve;
        options.Converters.Add(Create<JsonStringEnumConverter>());
        options.Converters.Add(Create<TypeJsonConverter>());
        options.Converters.Add(JsonMetadataServices.TimeSpanConverter);

        // Give external packages a chance to further configure the serializer options. E.g. to add additional converters.
        foreach (var configurator in _configurators) configurator.Configure(options);
    }

    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}