using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Models;

public record RelationalStructuredLogRecord
{
    public string Id { get; init; } = default!;
    public long Sequence { get; init; }
    public string Timestamp { get; init; } = default!;
    public string ReceivedAt { get; init; } = default!;
    public StructuredLogLevel Level { get; init; }
    public string Category { get; init; } = default!;
    public int EventId { get; init; }
    public string? EventName { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? MessageTemplate { get; init; }
    public string? ExceptionJson { get; init; }
    public string ScopesJson { get; init; } = "{}";
    public string PropertiesJson { get; init; } = "{}";
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public string? CorrelationId { get; init; }
    public string? TenantId { get; init; }
    public string? WorkflowDefinitionId { get; init; }
    public string? WorkflowInstanceId { get; init; }
    public string SourceId { get; init; } = default!;
}
