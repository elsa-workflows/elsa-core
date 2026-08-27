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

    /// <summary>Maps a library analysis onto the API model.</summary>
    public static BpmnImportAnalysisModel From(BpmnImportAnalysis analysis) => new()
    {
        ProcessIds = analysis.ProcessIds.ToList(),
        ElementCounts = new Dictionary<string, int>(analysis.ElementCounts),
        Issues = analysis.Issues.Select(BpmnImportIssueModel.From).ToList()
    };
}
