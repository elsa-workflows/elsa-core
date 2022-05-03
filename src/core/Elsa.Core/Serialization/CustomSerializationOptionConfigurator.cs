using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Serialization.Converters;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Serialization;

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