using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Elsa.Common.Services;

/// <inheritdoc />
public class JsonFormatter : IFormatter
{
    private readonly JsonSerializerOptions _options;
    
    public JsonFormatter()
    {
        _options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };
        _options.Converters.Add(new JsonStringEnumConverter());
    }
    
    /// <inheritdoc />
    public ValueTask<string> ToStringAsync(object value, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, _options);
        return ValueTask.FromResult(json);
    }

    /// <inheritdoc />
    public ValueTask<object> FromStringAsync(string data, Type? returnType, CancellationToken cancellationToken = default)
    {
        var value = returnType != null ? JsonSerializer.Deserialize(data, returnType, _options)! : JsonSerializer.Deserialize<object>(data, _options)!;
        return ValueTask.FromResult(value);
    }
}