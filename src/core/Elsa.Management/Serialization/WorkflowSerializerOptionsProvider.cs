using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Management.Contracts;
using Elsa.Management.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Management.Serialization;

public class WorkflowSerializerOptionsProvider
{
    private readonly IEnumerable<ISerializationOptionsConfigurator> _configurators;
    private readonly IServiceProvider _serviceProvider;
    
    public WorkflowSerializerOptionsProvider(IEnumerable<ISerializationOptionsConfigurator> configurators, IServiceProvider serviceProvider)
    {
        _configurators = configurators;
        _serviceProvider = serviceProvider;
    }

    public JsonSerializerOptions CreateApiOptions() => CreateDefaultOptions();
    public JsonSerializerOptions CreatePersistenceOptions() => CreateDefaultOptions(ReferenceHandler.Preserve);
    
    public JsonSerializerOptions CreateDefaultOptions(ReferenceHandler? referenceHandler = default)
    {
        var options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = referenceHandler,
            Converters =
            {
                Create<JsonStringEnumConverter>(),
                Create<TypeJsonConverter>(),
                Create<ActivityJsonConverterFactory>(),
                Create<ExpressionJsonConverterFactory>()
            }
        };

        // Give external packages a chance to further configure the serializer options. E.g. to add additional converters.
        foreach (var configurator in _configurators) configurator.Configure(options);

        return options;
    }

    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}