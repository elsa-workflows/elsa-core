using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Serialization.Converters;

public class OutputJsonConverterFactory : JsonConverterFactory
{
    private readonly IServiceProvider _serviceProvider;
    public OutputJsonConverterFactory(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;
    public override bool CanConvert(Type typeToConvert) => typeof(Output).IsAssignableFrom(typeToConvert);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var type = typeToConvert.GetGenericArguments().FirstOrDefault() ?? typeof(object);
        return (JsonConverter)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(OutputJsonConverter<>).MakeGenericType(type))!;
    }
}