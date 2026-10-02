using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// Reads the library-format <c>bpmnDefinitions</c> document the <c>bpmn/definitions/{definitionId}/document</c>
/// endpoints exchange (<c>Bpmn.Model</c> payload format 1.0.0; Studio's generated <c>BpmnDefinitions</c> type in the
/// Designer ClientLib's <c>src/bpmn/types.generated.ts</c> describes it). The document is held as a
/// <see cref="JsonObject"/> and edited in place, so everything Studio does not touch — foreign extensions, foreign
/// attributes, documentation, BPMN DI — goes back to the server exactly as it came.
/// </summary>
public static class BpmnDefinitionsDocument
{
    /// <summary>
    /// The BPMN element with <paramref name="elementId"/>, from any process's <c>elements</c>, or
    /// <see langword="null"/> when the document has none.
    /// </summary>
    /// <remarks>
    /// Only the elements of the document's own <c>&lt;process&gt;</c> elements are here. The body of a subprocess is
    /// not part of this document at all — <c>Bpmn.Interchange</c>'s reader carries it only in the work bindings it
    /// returns alongside, which the document endpoints do not send — so an element inside one is never found. Editing
    /// such an element is a server limitation, tracked in elsa-workflows/elsa-core#8076.
    /// </remarks>
    public static JsonObject? FindElement(JsonObject document, string elementId) =>
        Elements(document).FirstOrDefault(element => element["elementId"]?.GetValue<string>() == elementId);

    /// <summary>
    /// Whether <paramref name="processId"/> names one of the document's own <c>&lt;process&gt;</c> elements — the
    /// scope a top-level BPMN element lives in — rather than a nested one (a subprocess, transaction, or event
    /// subprocess), whose id never appears here because its body is not part of this document at all.
    /// </summary>
    public static bool HasTopLevelProcess(JsonObject document, string processId) =>
        (document["processes"] as JsonArray ?? [])
        .OfType<JsonObject>()
        .Any(process => process["processId"]?.GetValue<string>() == processId);

    private static IEnumerable<JsonObject> Elements(JsonObject document) =>
        (document["processes"] as JsonArray ?? [])
        .OfType<JsonObject>()
        .SelectMany(process => (process["elements"] as JsonArray ?? []).OfType<JsonObject>());
}
