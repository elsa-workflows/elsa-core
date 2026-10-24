using Bpmn.Interchange;
using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>What a document contains and how much of it survives a read, as reported over the API.</summary>
[PublicAPI]
public sealed class BpmnImportAnalysisModel
{
    /// <summary>The <c>id</c> of every <c>&lt;process&gt;</c> in the document, in document order.</summary>
    public IReadOnlyCollection<string> ProcessIds { get; init; } = [];

    /// <summary>How many of each BPMN local name were encountered, across all containers.</summary>
    public IReadOnlyDictionary<string, int> ElementCounts { get; init; } = new Dictionary<string, int>();

    /// <summary>Every finding, in the order the reader produced it.</summary>
    public IReadOnlyCollection<BpmnImportIssueModel> Issues { get; init; } = [];

    /// <summary>
    /// Why an import of the document would be refused, or null when it would be imported. Only <c>bpmn/analyze</c>
    /// reports it; an import that succeeded has nothing to report.
    /// </summary>
    public BpmnImportRefusalModel? Refusal { get; init; }

    /// <summary>Maps a library analysis onto the API model.</summary>
    public static BpmnImportAnalysisModel From(BpmnImportAnalysis analysis) => From(analysis, null);

    /// <summary>Maps a library analysis, and the refusal an import of the same document would raise, onto the API model.</summary>
    public static BpmnImportAnalysisModel From(BpmnImportAnalysis analysis, BpmnImportRefusalModel? refusal) => new()
    {
        ProcessIds = analysis.ProcessIds.ToList(),
        ElementCounts = new Dictionary<string, int>(analysis.ElementCounts),
        Issues = analysis.Issues.Select(BpmnImportIssueModel.From).ToList(),
        Refusal = refusal
    };
}
