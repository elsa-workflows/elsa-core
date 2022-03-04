using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Elsa.Runtime.ProtoActor.Messages;

namespace Elsa.Runtime.ProtoActor.Extensions;

public static class ProtoInputExtensions
{
    public static IReadOnlyDictionary<string, object?> Deserialize(this Input input) => input.Data.ToDictionary(x => x.Key, x => JsonSerializer.Deserialize<object>(x.Value.Text));

    public static Input Serialize(this IReadOnlyDictionary<string, object?> input)
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