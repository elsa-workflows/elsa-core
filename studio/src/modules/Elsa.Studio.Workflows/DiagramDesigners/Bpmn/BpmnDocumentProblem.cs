using System.Text.Json;
using Elsa.Studio.Bpmn.Designers;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// A problem with a BPMN document as the server analyzed it: why its import would be refused, or content its read
/// degrades or drops.
/// </summary>
/// <param name="Severity">How serious it is: <see cref="BpmnDesignerMarker.Error"/> for a refusal, otherwise <see cref="BpmnDesignerMarker.Warning"/>.</param>
/// <param name="Message">What is wrong, as the server put it.</param>
/// <param name="ElementIds">The elements and sequence flows it is about; empty when it names none.</param>
public sealed record BpmnDocumentProblem(string Severity, string Message, IReadOnlyList<string> ElementIds)
{
    /// <summary>
    /// The problems an analysis reports: its refusal first, then the findings that lose authored meaning, then each task
    /// bound to no activity.
    /// </summary>
    public static IReadOnlyList<BpmnDocumentProblem> From(BpmnImportAnalysisModel analysis)
    {
        var problems = new List<BpmnDocumentProblem>();

        if (analysis.Refusal is { } refusal)
        {
            problems.Add(new(BpmnDesignerMarker.Error, refusal.Message, refusal.ElementIds.ToList()));
        }

        // Info findings report what was kept, so they are not problems.
        foreach (var issue in analysis.Issues.Where(issue => !string.Equals(issue.Severity, "Info", StringComparison.OrdinalIgnoreCase)))
        {
            problems.Add(new(BpmnDesignerMarker.Warning, issue.Message, issue.ElementId is { } elementId ? [elementId] : []));
        }

        foreach (var elementId in analysis.UnboundElementIds)
        {
            problems.Add(new(BpmnDesignerMarker.Warning, $"Task '{elementId}' is not bound to an activity, so the workflow cannot be published until one is chosen under Performed by.", [elementId]));
        }

        return problems;
    }

    /// <summary>The problems an analysis the server refused reports: each error it gave, with the elements its data names.</summary>
    public static IReadOnlyList<BpmnDocumentProblem> From(ValidationErrors errors)
    {
        IReadOnlyList<string> elementIds = errors.Data is { ValueKind: JsonValueKind.Object } data
            && data.TryGetProperty("elementIds", out var ids)
            && ids.ValueKind == JsonValueKind.Array
                ? ids.EnumerateArray().Where(id => id.ValueKind == JsonValueKind.String).Select(id => id.GetString()!).ToList()
                : [];

        return errors.Errors.Select(error => new BpmnDocumentProblem(BpmnDesignerMarker.Error, error.ErrorMessage, elementIds)).ToList();
    }
}
