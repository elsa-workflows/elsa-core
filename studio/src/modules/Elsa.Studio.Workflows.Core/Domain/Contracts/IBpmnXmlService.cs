using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Loads, saves and creates a BPMN workflow's document as BPMN 2.0 XML, the way a BPMN designer edits it, through the
/// backend's <c>bpmn/definitions/{definitionId}/xml</c> and <c>bpmn/definitions</c> endpoints.
/// </summary>
public interface IBpmnXmlService
{
    /// <summary>Reads the definition's BPMN document and the <c>ETag</c> a save of an edit to it must send back.</summary>
    /// <param name="definitionId">The workflow definition whose document to read.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<Result<BpmnXmlRevision, BpmnDocumentFailure>> GetXmlAsync(string definitionId, CancellationToken cancellationToken = default);

    /// <summary>Saves <paramref name="xml"/> as the definition's BPMN document, if the stored one is still the revision <paramref name="eTag"/> names.</summary>
    /// <param name="definitionId">The workflow definition the document belongs to.</param>
    /// <param name="xml">The complete document.</param>
    /// <param name="eTag">The <c>ETag</c> of the revision the edit was made against.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>> PutXmlAsync(string definitionId, string xml, string eTag, CancellationToken cancellationToken = default);

    /// <summary>Creates a BPMN workflow named <paramref name="name"/>, holding a minimal process to edit.</summary>
    /// <param name="name">The workflow's name, which its process also takes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<Result<BpmnXmlSaveResult, BpmnDocumentFailure>> CreateAsync(string name, CancellationToken cancellationToken = default);
}
