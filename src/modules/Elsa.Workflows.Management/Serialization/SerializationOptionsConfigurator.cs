using System.Text.Json;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Serialization;

/// <summary>
/// Configures the JSON serialization options with support for serializing and deserializing activities and expressions.
/// </summary>
public class SerializationOptionsConfigurator : ISerializationOptionsConfigurator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializationOptionsConfigurator"/> class.
    /// </summary>
    public SerializationOptionsConfigurator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(Create<ActivityJsonConverterFactory>());
        options.Converters.Add(Create<ExpressionJsonConverterFactory>());
    }
    
    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}