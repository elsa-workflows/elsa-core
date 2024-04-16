using System.Diagnostics.CodeAnalysis;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.Services;

/// <inheritdoc />
public class BookmarkHasher(IHasher hasher) : IBookmarkHasher
{
    /// <inheritdoc />
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize(Object, Type, JsonSerializerOptions)")]
    public string Hash(string activityTypeName, object? payload, string? activityInstanceId = default)
    {
        return hasher.Hash(activityTypeName, payload, activityInstanceId);
    }
}