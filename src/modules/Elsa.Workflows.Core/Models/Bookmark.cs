using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Models;

public record Bookmark(
    string Id,
    string Name,
    string Hash,
    string? Data,
    string ActivityId,
    string ActivityInstanceId,
    string? CallbackMethodName = default
)
{
    [JsonConstructor]
    public Bookmark() : this("", "", "", null, "", "")
    {
    }
}