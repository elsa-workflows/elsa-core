namespace Elsa.Models;

public record Bookmark(
    string Id,
    string Name,
    string? Hash,
    string? Data,
    string ActivityId,
    string ActivityInstanceId,
    string? CallbackMethodName = default
);