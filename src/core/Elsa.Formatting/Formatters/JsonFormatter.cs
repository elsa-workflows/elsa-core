using System.Text.Json;
using Elsa.Formatting.Contracts;

namespace Elsa.Formatting.Formatters;

public class JsonFormatter : IFormatter
{
    public ValueTask<string> ToStringAsync(object body, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(body);
        return ValueTask.FromResult<string>(json);
    }

    public ValueTask<object> FromStringAsync(string data, Type? returnType, CancellationToken cancellationToken)
    {
        var options = new JsonSerializerOptions();
        var value = returnType != null ? JsonSerializer.Deserialize(data, returnType, options)! : JsonSerializer.Deserialize<object>(data, options)!;
        return ValueTask.FromResult(value);
    }
}