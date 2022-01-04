using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Management.Serialization.Converters;

public class InputJsonConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _serviceProvider;
    public InputJsonConverterFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    public override bool CanConvert(Type typeToConvert) => typeof(Input).IsAssignableFrom(typeToConvert);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var type = typeToConvert.GetGenericArguments().First();
        return (JsonConverter)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(InputJsonConverter<>).MakeGenericType(type))!;
    }
}