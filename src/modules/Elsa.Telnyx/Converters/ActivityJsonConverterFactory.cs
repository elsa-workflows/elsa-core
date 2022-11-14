using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Telnyx.Payloads.Abstract;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Telnyx.Converters;

public class PayloadJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Payload);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) => new PayloadJsonConverter();
}