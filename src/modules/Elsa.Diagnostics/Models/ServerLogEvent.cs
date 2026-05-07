namespace Elsa.Diagnostics.Models;

public record ServerLogEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public long Sequence { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public ServerLogLevel Level { get; init; }
    public string Category { get; init; } = default!;
    public int EventId { get; init; }
    public string? EventName { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? MessageTemplate { get; init; }
    public ServerLogException? Exception { get; init; }
    public IDictionary<string, string?> Scopes { get; init; } = new Dictionary<string, string?>();
    public IDictionary<string, string?> Properties { get; init; } = new Dictionary<string, string?>();
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public string? CorrelationId { get; init; }
    public string? TenantId { get; init; }
    public string? WorkflowDefinitionId { get; init; }
    public string? WorkflowInstanceId { get; init; }
    public string SourceId { get; init; } = default!;
}
