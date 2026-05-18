namespace Elsa.Diagnostics.ConsoleLogs.Models;

public record ConsoleLogSource
{
    public string Id { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string? ServiceName { get; init; }
    public int ProcessId { get; init; } = Environment.ProcessId;
    public string MachineName { get; init; } = Environment.MachineName;
    public string? PodName { get; init; }
    public string? ContainerName { get; init; }
    public string? Namespace { get; init; }
    public string? NodeName { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? LastSeen { get; init; }
    public ConsoleLogSourceHealth Health { get; init; } = ConsoleLogSourceHealth.Unknown;
    public IDictionary<string, string?> Metadata { get; init; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
}
