using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.UserTasks.Models;

namespace Elsa.UserTasks.Helpers;

/// <summary>
/// Reads a <see cref="UserTaskMaterialization"/> from a bookmark payload. Payloads loaded from storage may come back
/// camelCased, as a <see cref="JsonElement"/> or as an untyped property bag, so property names are matched case-insensitively.
/// </summary>
internal static class UserTaskBookmarkPayload
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static UserTaskMaterialization? ReadMaterialization(object? payload)
    {
        try
        {
            return payload switch
            {
                UserTaskMaterialization materialization => materialization,
                JsonElement element => element.Deserialize<UserTaskMaterialization>(SerializerOptions),
                null => null,
                _ => JsonSerializer.Deserialize<UserTaskMaterialization>(JsonSerializer.Serialize(payload), SerializerOptions)
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
