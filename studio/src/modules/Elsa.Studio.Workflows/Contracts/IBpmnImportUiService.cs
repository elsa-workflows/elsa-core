using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Contracts;

/// <summary>
/// Runs the interactive BPMN import flow for a single <c>.bpmn</c> file: analyze, show the findings for
/// confirmation (prompting for a process id when the document declares more than one), then import.
/// </summary>
public interface IBpmnImportUiService
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="file"/>'s name has a <c>.bpmn</c> extension,
    /// case-insensitively.
    /// </summary>
    bool IsBpmnFile(IBrowserFile file);

    /// <summary>
    /// Analyzes <paramref name="file"/>, shows its findings for confirmation, and imports it once confirmed.
    /// </summary>
    /// <param name="file">The <c>.bpmn</c> file to import.</param>
    /// <param name="definitionId">The workflow definition to update, or <see langword="null"/> to create a new one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The import outcome, or <see langword="null"/> when the user cancelled the confirmation dialog before anything
    /// was imported.
    /// </returns>
    Task<WorkflowImportResult?> ImportFileAsync(IBrowserFile file, string? definitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Splits <paramref name="files"/> into <c>.bpmn</c> files and everything else, then runs <see cref="ImportFileAsync"/>
    /// on each <c>.bpmn</c> file in turn. Shared by every entry point that accepts a mixed file selection, so the
    /// split-then-import-then-merge sequence is written once.
    /// </summary>
    /// <param name="files">The selected files, of any kind.</param>
    /// <param name="definitionId">The workflow definition to update, or <see langword="null"/> to create a new one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The BPMN import results and the files that were not <c>.bpmn</c>, for the caller to import itself.</returns>
    Task<BpmnImportBatch> ImportBpmnFilesAsync(IReadOnlyList<IBrowserFile> files, string? definitionId, CancellationToken cancellationToken = default);
}
