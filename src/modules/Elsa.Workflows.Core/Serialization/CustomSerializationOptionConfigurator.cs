using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Serialization;

/// <summary>
/// Add additional <see cref="JsonConverter"/> objects.
/// </summary>
public class CustomSerializationOptionConfigurator : ISerializationOptionsConfigurator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomSerializationOptionConfigurator"/> class.
    /// </summary>
    public CustomSerializationOptionConfigurator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(Create<VariableConverterFactory>());
    }
    
    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}