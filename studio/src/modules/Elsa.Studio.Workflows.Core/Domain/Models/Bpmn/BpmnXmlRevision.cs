namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// A workflow definition's BPMN document as the XML <c>GET</c> returned it, and the strong <c>ETag</c> naming that
/// revision, which a save of an edit to it must send back verbatim as <c>If-Match</c>.
/// </summary>
/// <param name="Xml">The document, exactly as stored.</param>
/// <param name="ETag">The opaque strong <c>ETag</c>, quotes included.</param>
public sealed record BpmnXmlRevision(string Xml, string ETag);
