using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// The outcome of running every <c>.bpmn</c> file in a mixed file selection through the interactive BPMN import
/// flow, plus whichever files were not <c>.bpmn</c> and so were left for the caller to import through its own
/// (JSON/ZIP) path.
/// </summary>
/// <param name="Results">
/// One <see cref="WorkflowImportResult"/> per <c>.bpmn</c> file that was imported, in selection order. A file whose
/// findings dialog was cancelled before anything was imported contributes no entry.
/// </param>
/// <param name="OtherFiles">Every selected file that was not a <c>.bpmn</c> file, unmodified.</param>
public record BpmnImportBatch(IReadOnlyList<WorkflowImportResult> Results, IReadOnlyList<IBrowserFile> OtherFiles)
{
    /// <summary>
    /// Filters <paramref name="results"/> down to the ones a caller should still report itself, i.e. everything
    /// except a capability refusal, which <see cref="Elsa.Studio.Workflows.Services.BpmnImportUiService"/> already
    /// reported in its own dialog. Callers typically pass the concatenation of <see cref="Results"/> with whatever
    /// they imported from <see cref="OtherFiles"/>.
    /// </summary>
    /// <param name="results">The results to filter.</param>
    /// <returns>The results that were not already reported in their own dialog.</returns>
    public static IReadOnlyList<WorkflowImportResult> ReportableResults(IEnumerable<WorkflowImportResult> results) =>
        results.Where(x => x.Failure?.FailureType != WorkflowImportFailureType.CapabilityRefusal).ToList();

    /// <summary>
    /// Determines whether every result in <paramref name="results"/> was already reported in its own dialog (i.e.
    /// every one was a capability refusal), meaning there is nothing left for the caller to summarize.
    /// </summary>
    /// <param name="results">The results to inspect.</param>
    /// <returns><c>true</c> when <paramref name="results"/> is non-empty and none of it is reportable.</returns>
    public static bool AllReported(IReadOnlyList<WorkflowImportResult> results) =>
        results.Count > 0 && ReportableResults(results).Count == 0;
}
