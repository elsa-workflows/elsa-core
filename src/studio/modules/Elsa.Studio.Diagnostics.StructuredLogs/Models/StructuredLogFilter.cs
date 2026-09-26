namespace Elsa.Studio.Diagnostics.StructuredLogs.Models;

/// <summary>
/// Structured log query and live subscription filter.
/// </summary>
public class StructuredLogFilter
{
    public StructuredLogLevel? MinimumLevel { get; set; } = StructuredLogLevel.Information;
    public ICollection<StructuredLogLevel>? Levels { get; set; }
    public string? CategoryPrefix { get; set; }
    public string? Text { get; set; }
    public string? TenantId { get; set; }
    public string? WorkflowDefinitionId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? CorrelationId { get; set; }
    public string? SourceId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int? Take { get; set; }
}
