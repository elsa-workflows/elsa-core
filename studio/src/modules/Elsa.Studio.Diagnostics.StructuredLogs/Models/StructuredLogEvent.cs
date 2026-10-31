namespace Elsa.Studio.Diagnostics.StructuredLogs.Models;

/// <summary>
/// A single structured log event received from the backend.
/// </summary>
public class StructuredLogEvent
{
    public string Id { get; set; } = default!;
    public DateTimeOffset Timestamp { get; set; }
    public StructuredLogLevel Level { get; set; }
    public string Category { get; set; } = default!;
    public string Message { get; set; } = default!;
    public string? MessageTemplate { get; set; }
    public int? EventId { get; set; }
    public string? EventName { get; set; }
    public StructuredLogException? Exception { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? CorrelationId { get; set; }
    public string? TenantId { get; set; }
    public string? WorkflowDefinitionId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string SourceId { get; set; } = default!;
    public IDictionary<string, string?> Properties { get; set; } = new Dictionary<string, string?>();
    public IDictionary<string, string?> Scopes { get; set; } = new Dictionary<string, string?>();
}
