using System.Text.Json;

namespace Elsa.Management.Serialization.Extensions;

public static class JsonSerializerExtensions
{
    public static T? DeserializeAnonymousType<T>(string json, T anonymousTypeObject, JsonSerializerOptions? options = default)
        => JsonSerializer.Deserialize<T>(json, options);

    public static ValueTask<TValue?> DeserializeAnonymousTypeAsync<TValue>(Stream stream, TValue anonymousTypeObject, JsonSerializerOptions? options = default, CancellationToken cancellationToken = default)
        => JsonSerializer.DeserializeAsync<TValue>(stream, options, cancellationToken);
}