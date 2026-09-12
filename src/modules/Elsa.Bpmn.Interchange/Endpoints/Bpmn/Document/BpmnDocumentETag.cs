using Elsa.Bpmn.Interchange.Services;
using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document;

/// <summary>
/// The strong <c>ETag</c> the document <c>Get</c> and <c>Put</c> endpoints exchange for optimistic concurrency,
/// derived from the workflow definition's own <c>Version</c>, its
/// <see cref="BpmnInterchangeDocumentService.SourceVersionCustomPropertyKey"/> custom property, and its
/// <see cref="BpmnInterchangeDocumentService.DocumentRevisionCustomPropertyKey"/> custom property.
/// </summary>
/// <remarks>
/// <c>Version</c> and <see cref="BpmnInterchangeDocumentService.SourceVersionCustomPropertyKey"/> are the same
/// revision notion <c>Export</c>'s staleness check already uses, so they are included here too rather than
/// inventing an unrelated one. Neither is guaranteed to change on every save, though — an unpublished draft is
/// edited in place, keeping the same version across repeated saves — so
/// <see cref="BpmnInterchangeDocumentService.DocumentRevisionCustomPropertyKey"/> is included specifically to
/// guarantee that a successful <c>PUT</c> always produces a different <c>ETag</c> from the one it required as
/// <c>If-Match</c>. Both endpoints only ever compute this once <see cref="BpmnInterchangeDocumentService.ReadDocument"/>
/// or <see cref="BpmnInterchangeDocumentService.ImportDocumentAsync"/> has already succeeded.
/// </remarks>
internal static class BpmnDocumentETag
{
    /// <summary>Computes the quoted strong ETag for <paramref name="definition"/>'s current revision.</summary>
    public static string From(WorkflowDefinition definition)
    {
        var sourceVersion = definition.CustomProperties.TryGetValue<int>(BpmnInterchangeDocumentService.SourceVersionCustomPropertyKey, out var version)
            ? version
            : -1;

        var documentRevision = definition.CustomProperties.TryGetValue<int>(BpmnInterchangeDocumentService.DocumentRevisionCustomPropertyKey, out var revision)
            ? revision
            : -1;

        return $"\"{definition.Version}-{sourceVersion}-{documentRevision}\"";
    }
}
