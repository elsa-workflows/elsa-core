using System.Text.Json;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Serialization;

public class SerializationOptionsConfigurator : ISerializationOptionsConfigurator
{
    private readonly IServiceProvider _serviceProvider;

    public SerializationOptionsConfigurator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(Create<ActivityJsonConverterFactory>());
        options.Converters.Add(Create<ExpressionJsonConverterFactory>());
    }
    
    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}