using Refit;

namespace Elsa.Studio.Workflows.Client;

/// <summary>
/// The BPMN XML endpoints, through which a BPMN designer loads and saves a workflow's BPMN document, and creates a new
/// BPMN workflow.
/// </summary>
/// <remarks>
/// Each call returns the raw response so the <c>ETag</c> header can be read, and so a document is exchanged as the
/// exact text the designer produced, never through Studio's API-wide serializer.
/// </remarks>
public interface IBpmnXmlApi
{
    /// <summary>
    /// Reads the latest version's stored BPMN document exactly as stored, as <c>application/xml</c>, with a strong
    /// <c>ETag</c> header naming the revision.
    /// </summary>
    /// <param name="definitionId">The workflow definition whose document to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Get("/bpmn/definitions/{definitionId}/xml")]
    Task<HttpResponseMessage> GetXmlAsync(string definitionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a complete BPMN document as the definition's source: the server re-imports it into the same definition as
    /// a draft, preserving the definition's non-BPMN metadata.
    /// </summary>
    /// <param name="definitionId">The workflow definition the document belongs to.</param>
    /// <param name="xml">The document, as UTF-8 <c>application/xml</c>.</param>
    /// <param name="ifMatch">The <c>ETag</c> of the revision the edit was made against, sent back verbatim.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Put("/bpmn/definitions/{definitionId}/xml")]
    Task<HttpResponseMessage> PutXmlAsync(string definitionId, [Body] HttpContent xml, [Header("If-Match")] string ifMatch, CancellationToken cancellationToken = default);

    /// <summary>Creates a BPMN workflow from scratch: a draft holding a minimal process, with the <c>ETag</c> its first save needs.</summary>
    /// <param name="request">A JSON body with the workflow's <c>name</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Post("/bpmn/definitions")]
    Task<HttpResponseMessage> CreateAsync([Body] HttpContent request, CancellationToken cancellationToken = default);
}
