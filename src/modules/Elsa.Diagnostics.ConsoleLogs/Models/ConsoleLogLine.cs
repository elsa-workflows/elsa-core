namespace Elsa.Diagnostics.ConsoleLogs.Models;

public record ConsoleLogLine
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ReceivedAt { get; init; } = DateTimeOffset.UtcNow;
    public long Sequence { get; init; }
    public ConsoleLogStream Stream { get; init; }
    public string Text { get; init; } = string.Empty;
    public ConsoleLogSource Source { get; init; } = default!;
    public string? WorkflowInstanceId { get; init; }
    public bool Truncated { get; init; }
    public ConsoleLogDroppedSummary? Dropped { get; init; }
}
