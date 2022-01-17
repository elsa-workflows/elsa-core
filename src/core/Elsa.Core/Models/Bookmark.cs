namespace Elsa.Models;

public record Bookmark(
    string Id,
    string Name,
    string? Hash,
    string? Payload,
    string ActivityId,
    string ActivityInstanceId,
    string? CallbackMethodName = default
);