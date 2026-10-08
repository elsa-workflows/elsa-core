using Bpmn.Interchange;
using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>One element-scoped finding from a BPMN read, as reported over the API.</summary>
[PublicAPI]
public sealed class BpmnImportIssueModel
{
    /// <summary>How much of the element's authored meaning survived the read: <c>Info</c>, <c>Degraded</c> or <c>Dropped</c>.</summary>
    public string Severity { get; init; } = string.Empty;

    /// <summary>A sentence naming the element, what the document said, and what the reader did about it.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>The BPMN id the finding is about, when it is about one element.</summary>
    public string? ElementId { get; init; }

    /// <summary>The process the finding occurred in, when it is scoped to one.</summary>
    public string? ProcessId { get; init; }

    /// <summary>Maps a library finding onto the API model.</summary>
    public static BpmnImportIssueModel From(BpmnImportIssue issue) => new()
    {
        Severity = issue.Severity.ToString(),
        Message = issue.Message,
        ElementId = issue.ElementId,
        ProcessId = issue.ProcessId
    };
}
