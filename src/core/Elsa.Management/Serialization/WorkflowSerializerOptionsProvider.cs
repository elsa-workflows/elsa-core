using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Management.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Management.Serialization;

public class WorkflowSerializerOptionsProvider
{
    private readonly IServiceProvider _serviceProvider;
    public WorkflowSerializerOptionsProvider(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public JsonSerializerOptions CreateSerializerOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            Create<JsonStringEnumConverter>(),
            Create<TypeJsonConverter>(),
            Create<ActivityJsonConverterFactory>(),
            Create<TriggerJsonConverterFactory>(),
            Create<FlowchartJsonConverter>()
        }
    };

    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}