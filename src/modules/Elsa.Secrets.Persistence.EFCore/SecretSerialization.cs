using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Secrets.Models;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Secrets.Persistence.EFCore;

internal static class SecretSerialization
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static void StoreSerializedProperties(DbContext dbContext, Secret secret)
    {
        dbContext.Entry(secret).Property(SecretShadowPropertyNames.SerializedTags).CurrentValue = JsonSerializer.Serialize(secret.Tags.Order(StringComparer.OrdinalIgnoreCase), JsonOptions);
        dbContext.Entry(secret).Property(SecretShadowPropertyNames.SerializedVersions).CurrentValue = JsonSerializer.Serialize(secret.Versions, JsonOptions);
    }

    public static void LoadSerializedProperties(DbContext dbContext, Secret? secret)
    {
        if (secret == null)
            return;

        var tagsJson = dbContext.Entry(secret).Property<string>(SecretShadowPropertyNames.SerializedTags).CurrentValue;
        var versionsJson = dbContext.Entry(secret).Property<string>(SecretShadowPropertyNames.SerializedVersions).CurrentValue;
        var tags = Deserialize(tagsJson, static () => new List<string>());
        var versions = Deserialize(versionsJson, static () => new List<SecretVersion>());
        secret.Tags = tags.ToHashSet(StringComparer.OrdinalIgnoreCase);
        secret.Versions = versions;
    }

    private static T Deserialize<T>(string? json, Func<T> fallback)
    {
        if (string.IsNullOrWhiteSpace(json))
            return fallback();

        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback();
    }
}
