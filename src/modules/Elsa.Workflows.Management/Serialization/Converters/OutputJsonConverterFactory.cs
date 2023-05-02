using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Serialization.Converters;

/// <summary>
/// A JSON converter factory that creates <see cref="OutputJsonConverter{T}"/> instances.
/// </summary>
public class OutputJsonConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc />
    public OutputJsonConverterFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(Output).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var type = typeToConvert.GetGenericArguments().FirstOrDefault() ?? typeof(object);
        return (JsonConverter)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(OutputJsonConverter<>).MakeGenericType(type))!;
    }
}