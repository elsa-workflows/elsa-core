using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Serialization.Converters;

/// <summary>
/// A JSON converter factory that creates <see cref="InputJsonConverter{T}"/> instances.
/// </summary>
public class InputJsonConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <inheritdoc />
    public InputJsonConverterFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => typeof(Input).IsAssignableFrom(typeToConvert);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var type = typeToConvert.GetGenericArguments().First();
        return (JsonConverter)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(InputJsonConverter<>).MakeGenericType(type))!;
    }
}