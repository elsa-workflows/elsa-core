using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common.Contracts;

namespace Elsa.Common.Services;

/// <inheritdoc />
public class JsonFormatter : IFormatter
{
    /// <inheritdoc />
    public ValueTask<string> ToStringAsync(object value, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        return ValueTask.FromResult(json);
    }

    /// <inheritdoc />
    public ValueTask<object> FromStringAsync(string data, Type? returnType, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonStringEnumConverter());
        var value = returnType != null ? JsonSerializer.Deserialize(data, returnType, options)! : JsonSerializer.Deserialize<object>(data, options)!;
        return ValueTask.FromResult(value);
    }
}