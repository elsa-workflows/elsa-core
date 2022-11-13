using System.Text.Json;
using Elsa.Runtime.Protos;

namespace Elsa.ProtoActor.Extensions;

public static class ProtoInputExtensions
{
    public static IDictionary<string, object> Deserialize(this Input input) => input.Data.ToDictionary(x => x.Key, x => JsonSerializer.Deserialize<object>(x.Value.Text)!);

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