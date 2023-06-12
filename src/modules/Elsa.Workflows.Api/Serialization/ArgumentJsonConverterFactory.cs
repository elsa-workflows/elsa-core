using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Api.Serialization;

internal class ArgumentJsonConverterFactory : JsonConverterFactory
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    public ArgumentJsonConverterFactory(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }
    
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsAssignableTo(typeof(ArgumentDefinition));

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new ArgumentJsonConverter(_wellKnownTypeRegistry);
    }
}