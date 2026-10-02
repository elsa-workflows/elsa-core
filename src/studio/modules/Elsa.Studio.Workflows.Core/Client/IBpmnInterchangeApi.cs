using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Refit;

namespace Elsa.Studio.Workflows.Client;

/// <summary>
/// Backend API for the BPMN interchange endpoints (<c>Elsa.Bpmn.Interchange</c>): analyzing, importing and
/// exporting BPMN 2.0 documents.
/// </summary>
public interface IBpmnInterchangeApi
{
    /// <summary>
    /// Reads a <c>.bpmn</c> document and reports the Info/Degraded/Dropped findings a read would produce, without
    /// persisting anything.
    /// </summary>
    [Multipart]
    [Post("/bpmn/analyze")]
    Task<BpmnImportAnalysisModel> AnalyzeAsync([AliasAs("file")] StreamPart file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a <c>.bpmn</c> document and persists it as a draft workflow definition.
    /// </summary>
    [Multipart]
    [Post("/bpmn/import")]
    Task<BpmnImportResultModel> ImportAsync(
        [AliasAs("file")] StreamPart file,
        [AliasAs("DefinitionId")] string? definitionId,
        [AliasAs("Name")] string? name,
        [AliasAs("ProcessId")] string? processId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the BPMN 2.0 XML a workflow definition was imported from.
    /// </summary>
    [Get("/bpmn/definitions/{definitionId}/export")]
    Task<HttpResponseMessage> ExportAsync(string definitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the whole <c>bpmnDefinitions</c> document a workflow definition was imported from, as the library's own
    /// JSON (plain <c>System.Text.Json</c> defaults: camelCase names as <c>Bpmn.Model</c> declares them, integer enums),
    /// with a strong <c>ETag</c> header naming the revision.
    /// </summary>
    /// <remarks>
    /// Returned raw so the body is read verbatim, never through Studio's API-wide serializer conventions, and so the
    /// <c>ETag</c> header can be read.
    /// </remarks>
    [Get("/bpmn/definitions/{definitionId}/document")]
    Task<HttpResponseMessage> GetDocumentAsync(string definitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes an edited <c>bpmnDefinitions</c> document back: the server writes it out as BPMN 2.0 XML and re-imports
    /// it into the same definition as a draft, preserving the definition's non-BPMN metadata.
    /// </summary>
    /// <param name="definitionId">The workflow definition whose document this is.</param>
    /// <param name="document">The document JSON, exactly the shape <see cref="GetDocumentAsync"/> returned.</param>
    /// <param name="ifMatch">The <c>ETag</c> of the revision the edit was made against, sent back verbatim.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Put("/bpmn/definitions/{definitionId}/document")]
    Task<HttpResponseMessage> PutDocumentAsync(string definitionId, [Body] HttpContent document, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);
}
