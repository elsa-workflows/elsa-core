using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Activities.Converters;
using Elsa.Management.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Configurators;

/// <summary>
/// Add additional <see cref="JsonConverter"/> objects.
/// </summary>
public class CustomSerializationOptionConfigurator : ISerializationOptionsConfigurator
{
    private readonly IServiceProvider _serviceProvider;

    public CustomSerializationOptionConfigurator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(Create<FlowchartJsonConverter>());
    }
    
    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}