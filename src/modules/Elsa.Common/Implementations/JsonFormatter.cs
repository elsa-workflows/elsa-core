using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common.Services;

namespace Elsa.Common.Implementations;

public class JsonFormatter : IFormatter
{
    public ValueTask<string> ToStringAsync(object body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        return ValueTask.FromResult(json);
    }

    public ValueTask<object> FromStringAsync(string data, Type? returnType, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        var value = returnType != null ? JsonSerializer.Deserialize(data, returnType, options)! : JsonSerializer.Deserialize<object>(data, options)!;
        return ValueTask.FromResult(value);
    }
}