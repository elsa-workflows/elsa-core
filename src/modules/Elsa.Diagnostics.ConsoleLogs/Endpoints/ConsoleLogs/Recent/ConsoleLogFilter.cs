namespace Elsa.Diagnostics.ConsoleLogs.Endpoints.ConsoleLogs.Recent;

internal sealed record ConsoleLogFilter
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
