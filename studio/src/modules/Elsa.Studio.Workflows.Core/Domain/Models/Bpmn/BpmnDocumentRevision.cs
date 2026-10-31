using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// A workflow definition's whole <c>bpmnDefinitions</c> document, as the document <c>GET</c> returned it, and the strong
/// <c>ETag</c> naming that revision — the value a later document <c>PUT</c> must send back verbatim as <c>If-Match</c>.
/// </summary>
/// <param name="Document">The document, held as JSON so everything Studio does not edit round-trips untouched.</param>
/// <param name="ETag">The opaque strong <c>ETag</c>, quotes included.</param>
public sealed record BpmnDocumentRevision(JsonObject Document, string ETag);
