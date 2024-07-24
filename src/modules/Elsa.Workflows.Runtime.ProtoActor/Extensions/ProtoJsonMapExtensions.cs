using System.Text.Json;
using Elsa.Workflows.Runtime.ProtoActor.ProtoBuf;
using Elsa.Workflows.Serialization.Converters;
using Google.Protobuf.Collections;

namespace Elsa.Workflows.Runtime.ProtoActor.Extensions;

internal static class ProtoJsonMapExtensions
{
    public static IDictionary<string, object> Deserialize(this MapField<string, Json> input)
    {
        var jsonSerializerOptions = new JsonSerializerOptions();
        jsonSerializerOptions.Converters.Add(new ExpandoObjectConverterFactory());

        return input.ToDictionary(x => x.Key, x => JsonSerializer.Deserialize<object>(x.Value.Text, jsonSerializerOptions)!);
    }

    public static void Serialize(this IDictionary<string, object> input, MapField<string, Json> target)
    {
        foreach (var (key, value) in input)
        {
            target[key] = new Json
            {
                Text = JsonSerializer.Serialize(value)
            };
        }
    }
}