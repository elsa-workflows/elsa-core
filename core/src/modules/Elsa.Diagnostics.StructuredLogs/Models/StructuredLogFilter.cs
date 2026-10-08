namespace Elsa.Diagnostics.StructuredLogs.Models;

public record StructuredLogFilter
{
    public StructuredLogLevel? MinimumLevel { get; init; }
    public ICollection<StructuredLogLevel>? Levels { get; init; }
    public string? CategoryPrefix { get; init; }
    public string? Text { get; init; }
    public string? TenantId { get; init; }
    public string? WorkflowDefinitionId { get; init; }
    public string? WorkflowInstanceId { get; init; }
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }
    public string? CorrelationId { get; init; }
    public string? SourceId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int? Take { get; init; }
}
