namespace Elsa.Diagnostics.ConsoleLogs.Contracts;

/// <summary>
/// Console log filter accepted by Elsa REST and SignalR endpoints.
/// </summary>
public sealed record ElsaConsoleLogFilter
{
    public string? SourceId { get; init; }
    public global::ConsoleLogStream.Core.Models.ConsoleStream? Stream { get; init; }
    public string? Query { get; init; }
    public string? WorkflowInstanceId { get; init; }
    public string? WorkflowDefinitionId { get; init; }
    public string? WorkflowDefinitionVersionId { get; init; }
    public string? ActivityInstanceId { get; init; }
    public string? ActivityId { get; init; }
    public string? ActivityNodeId { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int? Limit { get; init; }
}
