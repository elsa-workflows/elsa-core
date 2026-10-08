using System.Text.Json.Nodes;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Analyzes, imports and exports BPMN 2.0 documents through the backend's <c>Elsa.Bpmn.Interchange</c> endpoints.
/// </summary>
public interface IBpmnInterchangeService
{
    /// <summary>
    /// Reads a <c>.bpmn</c> document and reports the Info/Degraded/Dropped findings a read would produce, without
    /// persisting anything.
    /// </summary>
    /// <param name="content">The BPMN 2.0 XML document.</param>
    /// <param name="fileName">The uploaded file's name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The analysis on success, or the server's error messages on failure.</returns>
    Task<Result<BpmnImportAnalysisModel, ValidationErrors>> AnalyzeAsync(Stream content, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a <c>.bpmn</c> document and persists it as a draft workflow definition.
    /// </summary>
    /// <param name="content">The BPMN 2.0 XML document.</param>
    /// <param name="fileName">The uploaded file's name.</param>
    /// <param name="definitionId">The workflow definition to update, or <see langword="null"/> to create a new one.</param>
    /// <param name="name">The workflow definition's display name, defaulting to the process's own BPMN name or id.</param>
    /// <param name="processId">The process to import when the document declares more than one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The imported definition's identity and analysis on success, or the server's error messages on failure.</returns>
    Task<Result<BpmnImportResultModel, ValidationErrors>> ImportAsync(
        Stream content,
        string fileName,
        string? definitionId,
        string? name,
        string? processId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the BPMN 2.0 XML a workflow definition was imported from, ready to download.
    /// </summary>
    /// <param name="definitionId">The workflow definition id to export.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file to download on success, or the classified refusal reason on failure.</returns>
    Task<Result<FileDownload, BpmnExportFailure>> ExportAsync(string definitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the whole <c>bpmnDefinitions</c> document a workflow definition was imported from, with the <c>ETag</c>
    /// naming that revision.
    /// </summary>
    /// <param name="definitionId">The workflow definition id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The document and its <c>ETag</c> on success, or the classified refusal on failure.</returns>
    Task<Result<BpmnDocumentRevision, BpmnDocumentFailure>> GetDocumentAsync(string definitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes an edited <c>bpmnDefinitions</c> document back, refused unless <paramref name="eTag"/> still names the
    /// definition's current revision. The server re-imports it into the same definition as a draft.
    /// </summary>
    /// <param name="definitionId">The workflow definition id.</param>
    /// <param name="document">The whole document, as read by <see cref="GetDocumentAsync"/> and edited in place.</param>
    /// <param name="eTag">The <c>ETag</c> of the revision the edit was made against, sent as <c>If-Match</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The re-import's result and the new <c>ETag</c> on success, or the classified refusal on failure.</returns>
    Task<Result<BpmnDocumentSaveResult, BpmnDocumentFailure>> PutDocumentAsync(string definitionId, JsonObject document, string eTag, CancellationToken cancellationToken = default);
}
