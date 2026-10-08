using System.Globalization;
using System.Security.Cryptography;
using Elsa.Bpmn.Interchange.Services;
using Elsa.Extensions;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Document;

/// <summary>
/// The strong <c>ETag</c> the document <c>Get</c> and <c>Put</c> endpoints exchange for optimistic concurrency: a
/// SHA-256 hash over the stored workflow definition's id, its version, its BPMN source
/// (<see cref="BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey"/>) and its serialized activity graph
/// (<see cref="WorkflowDefinition.StringData"/>).
/// </summary>
/// <remarks>
/// <para>
/// Content-derived rather than a stored counter, because no counter survives every writer. An unpublished draft is
/// edited in place — same row, same version — and both <see cref="Elsa.Workflows.Management.IWorkflowDefinitionImporter"/>
/// (behind <c>Import</c> and the document <c>Put</c>) and the workflow-definition save endpoint the designer uses
/// replace <c>CustomProperties</c> wholesale with the caller's, so a counter kept there is wiped, or carried forward
/// unchanged, by exactly the writes it would have to record. What those writes do change is the content: a document
/// <c>Put</c> or an <c>Import</c> rewrites the stored source, a designer save rewrites the graph, and a save that drops
/// the source hashes differently from one that keeps it. The id and version tie the value to one stored row, so a new
/// draft version of identical content still gets a different one.
/// </para>
/// <para>
/// Identical stored content yields an identical <c>ETag</c>, which is what a strong validator means — it names a
/// representation — so a <c>Put</c> that writes back exactly what is stored returns the value <c>Get</c> did, having
/// overwritten nothing. Every input is length-prefixed and an absent one is marked distinctly from an empty one, so
/// no two different sets of inputs feed the hash the same bytes. The value is opaque to clients, which must send it
/// back verbatim.
/// </para>
/// </remarks>
internal static class BpmnDocumentETag
{
    /// <summary>Computes the quoted strong ETag for <paramref name="definition"/> as it is stored.</summary>
    public static string From(WorkflowDefinition definition)
    {
        var sourceXml = definition.CustomProperties.TryGetValue<string>(BpmnInterchangeDocumentService.SourceXmlCustomPropertyKey, out var xml) ? xml : null;

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        BpmnContentHash.AppendField(hash, definition.Id);
        BpmnContentHash.AppendField(hash, definition.Version.ToString(CultureInfo.InvariantCulture));
        BpmnContentHash.AppendField(hash, sourceXml);
        BpmnContentHash.AppendField(hash, definition.StringData);

        return $"\"{Convert.ToHexString(hash.GetHashAndReset())}\"";
    }
}
