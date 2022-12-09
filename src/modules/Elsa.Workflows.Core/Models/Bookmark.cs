using System.Text.Json.Serialization;

namespace Elsa.Workflows.Core.Models;

public record Bookmark(
    string Id,
    string Name,
    string Hash,
    string? Data,
    string ActivityId,
    string ActivityInstanceId,
    bool AutoBurn = true,
    string? CallbackMethodName = default
)
{
    [JsonConstructor]
    public Bookmark() : this("", "", "", null, "", "")
    {
    }
}