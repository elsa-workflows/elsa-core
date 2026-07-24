using System.Text.Json;

namespace Elsa.Persistence.EFCore.Modules.ExternalAuthentication;

internal static class ExternalAuthenticationJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value, Options)
        ?? throw new InvalidOperationException($"The persisted external authentication value could not be deserialized as {typeof(T).Name}.");
}
