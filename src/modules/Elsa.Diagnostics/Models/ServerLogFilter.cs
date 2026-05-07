namespace Elsa.Diagnostics.Models;

public record ServerLogFilter
{
    public ServerLogLevel? MinimumLevel { get; init; }
    public ICollection<ServerLogLevel>? Levels { get; init; }
    public string? CategoryPrefix { get; init; }
    public string? Text { get; init; }
    public string? TenantId { get; init; }
    public string? WorkflowDefinitionId { get; init; }
    public string? WorkflowInstanceId { get; init; }
    public string? TraceId { get; init; }
    public string? CorrelationId { get; init; }
    public string? SourceId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int? Take { get; init; }
}
