using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Activities.Flowchart.Serialization;

/// <summary>
/// Add additional <see cref="JsonConverter"/> objects.
/// </summary>
public class FlowchartSerializationOptionConfigurator : ISerializationOptionsConfigurator
{
    private readonly IServiceProvider _serviceProvider;
    public FlowchartSerializationOptionConfigurator(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    public void Configure(JsonSerializerOptions options) => options.Converters.Add(Create<FlowchartJsonConverter>());
    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}