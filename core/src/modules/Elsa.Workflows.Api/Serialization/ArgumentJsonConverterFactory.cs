using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Api.Serialization;

internal class ArgumentJsonConverterFactory(IWellKnownTypeRegistry wellKnownTypeRegistry) : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsAssignableTo(typeof(ArgumentDefinition));

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new ArgumentJsonConverter(wellKnownTypeRegistry);
    }
}