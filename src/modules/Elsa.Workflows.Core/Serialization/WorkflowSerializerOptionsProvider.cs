using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Dahomey.Json;
using Elsa.Workflows.Core.Serialization.Converters;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Serialization;

public class WorkflowSerializerOptionsProvider
{
    private readonly IEnumerable<ISerializationOptionsConfigurator> _configurators;
    private readonly IServiceProvider _serviceProvider;

    public WorkflowSerializerOptionsProvider(IEnumerable<ISerializationOptionsConfigurator> configurators, IServiceProvider serviceProvider)
    {
        _configurators = configurators;
        _serviceProvider = serviceProvider;
    }

    public JsonSerializerOptions CreateApiOptions() => CreateDefaultOptions(ReferenceHandling.Ignore);
    public JsonSerializerOptions CreatePersistenceOptions() => CreateDefaultOptions();

    public JsonSerializerOptions CreateDefaultOptions(ReferenceHandling referenceHandling = ReferenceHandling.Preserve)
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
        
        // Give external packages a chance to further configure the serializer options. E.g. to add additional converters.
        foreach (var configurator in _configurators) configurator.Configure(options);
        
        // Dahomey.
        // options.SetupExtensions();
        // options.SetReferenceHandling(referenceHandling);
        //
        // // Setup polymorphic serialization.
        // var registry = options.GetDiscriminatorConventionRegistry();
        // registry.RegisterConvention(new DefaultDiscriminatorConvention<string>(options));
        // registry.DiscriminatorPolicy = DiscriminatorPolicy.Auto;

        
        return options;
    }

    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}