using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Serialization.Converters;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Serialization.Configurators;

/// <summary>
/// Add additional <see cref="JsonConverter"/> objects.
/// </summary>
[UsedImplicitly]
public class AdditionalConvertersConfigurator(IServiceProvider serviceProvider) : SerializationOptionsConfiguratorBase
{
    /// <inheritdoc />
    public override void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(Create<VariableConverterFactory>());
    }

    private T Create<T>() => ActivatorUtilities.CreateInstance<T>(serviceProvider);
}