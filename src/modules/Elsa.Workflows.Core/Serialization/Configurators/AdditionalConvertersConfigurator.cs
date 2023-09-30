using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Serialization.Configurators;

/// <summary>
/// Add additional <see cref="JsonConverter"/> objects.
/// </summary>
public class AdditionalConvertersConfigurator : SerializationOptionsConfiguratorBase
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdditionalConvertersConfigurator"/> class.
    /// </summary>
    public AdditionalConvertersConfigurator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public override void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(Create<VariableConverterFactory>());
    }

    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(_serviceProvider);
}