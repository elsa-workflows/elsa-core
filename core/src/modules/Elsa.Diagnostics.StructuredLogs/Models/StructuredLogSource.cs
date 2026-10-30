namespace Elsa.Diagnostics.StructuredLogs.Models;

public record StructuredLogSource
{
    public string Id { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string? ServiceName { get; init; }
    public string MachineName { get; init; } = Environment.MachineName;
    public int ProcessId { get; init; } = Environment.ProcessId;
    public string? PodName { get; init; }
    public string? Namespace { get; init; }
    public string? ContainerName { get; init; }
    public string? NodeName { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? LastSeen { get; init; }
    public StructuredLogSourceStatus Status { get; init; } = StructuredLogSourceStatus.Unknown;
}
