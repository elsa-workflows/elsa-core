using System.Text.Json.Serialization;

namespace Elsa.Diagnostics.ConsoleLogs.Contracts;

/// <summary>
/// Console log filter accepted by Elsa REST and SignalR endpoints.
/// </summary>
public sealed class ElsaConsoleLogFilter
{
    public string? SourceId { get; set; }

    [JsonConverter(typeof(ConsoleStreamJsonConverter))]
    public global::ConsoleLogStreaming.Core.Models.ConsoleStream? Stream { get; set; }
    public string? Query { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? WorkflowDefinitionId { get; set; }
    public string? WorkflowDefinitionVersionId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public string? ActivityId { get; set; }
    public string? ActivityNodeId { get; set; }
    public IDictionary<string, string>? Metadata { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int? Limit { get; set; }
}
