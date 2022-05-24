namespace Elsa.Models;

public record WorkflowMetadata(string? Name = default, string? Description = default, DateTimeOffset CreatedAt = default)
{
}