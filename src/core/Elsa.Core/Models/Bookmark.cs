namespace Elsa.Models;

public record Bookmark(
    string Id,
    string Name,
    string? Hash,
    string ActivityId,
    string ActivityInstanceId,
    IDictionary<string, object?>? Data = default,
    string? CallbackMethodName = default
);