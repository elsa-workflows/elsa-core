using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Management.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Management.Serialization;

public class WorkflowSerializerOptionsProvider
{
    private readonly IServiceProvider _serviceProvider;
    public WorkflowSerializerOptionsProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

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
                Create<TriggerJsonConverterFactory>(),
                Create<ExpressionJsonConverterFactory>(),
                Create<FlowchartJsonConverter>()
            }
        };

        return options;
    }

    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}