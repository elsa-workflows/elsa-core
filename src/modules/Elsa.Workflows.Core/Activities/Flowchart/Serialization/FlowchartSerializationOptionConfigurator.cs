using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Activities.Flowchart.Serialization;

/// <summary>
/// Add additional <see cref="JsonConverter"/> objects.
/// </summary>
public class FlowchartSerializationOptionConfigurator : SerializationOptionsConfiguratorBase
{
    private readonly IServiceProvider _serviceProvider;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FlowchartSerializationOptionConfigurator"/> class.
    /// </summary>
    public FlowchartSerializationOptionConfigurator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public override void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(Create<FlowchartJsonConverter>());
    }

    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}