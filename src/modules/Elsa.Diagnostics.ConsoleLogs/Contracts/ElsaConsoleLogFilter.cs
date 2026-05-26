namespace Elsa.Diagnostics.ConsoleLogs.Contracts;

/// <summary>
/// Console log filter accepted by Elsa REST and SignalR endpoints.
/// </summary>
public sealed record ElsaConsoleLogFilter
{
    public string? SourceId { get; init; }
    public global::ConsoleLogStreaming.Contracts.ConsoleLogStreaming? Stream { get; init; }
    public string? Query { get; init; }
    public string? WorkflowInstanceId { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int? Limit { get; init; }
}
