using System.Text.Json;
using Elsa.ProtoActor.ProtoBuf;
using Elsa.Workflows.Core.Serialization.Converters;

namespace Elsa.ProtoActor.Extensions;

internal static class ProtoInputExtensions
{
    public static IDictionary<string, object> Deserialize(this Input input)
    {
        var jsonSerializerOptions = new JsonSerializerOptions();
        jsonSerializerOptions.Converters.Add(new ExpandoObjectConverterFactory());
        
        return input.Data.ToDictionary(x => x.Key, x => JsonSerializer.Deserialize<object>(x.Value.Text, jsonSerializerOptions)!);
    }

    public static Input Serialize(this IDictionary<string, object> input)
    {
        var result = new Input();
        var data = result.Data;

        foreach (var (key, value) in input)
        {
            data[key] = new Json
            {
                Text = JsonSerializer.Serialize(value)
            };
        }

        return result;
    }
}