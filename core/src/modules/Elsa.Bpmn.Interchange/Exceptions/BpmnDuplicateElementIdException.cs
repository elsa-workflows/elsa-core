namespace Elsa.Bpmn.Interchange.Exceptions;

/// <summary>
/// Thrown when a BPMN document declares the same element id more than once.
/// </summary>
/// <remarks>
/// BPMN requires every element id to be unique within a document. A repeat — most often a subprocess nested inside
/// another subprocess that reuses its parent's id — is not merely invalid input: <c>BpmnInterchangeDocumentService</c>'s
/// own capability walk, <c>BpmnWorkBinder.BindScope</c> and <c>Bpmn.Interchange</c>'s own <c>BpmnXmlWriter</c> all read
/// "the nested processes belonging to this scope" back out of a flat binding list by matching on the repeated id, so a
/// document like this makes each of them recurse without ever terminating and crash the process outright — .NET
/// cannot catch a <see cref="StackOverflowException"/>. This is thrown, and the document refused, before any of that
/// recursion runs.
/// </remarks>
public class BpmnDuplicateElementIdException(string message, IReadOnlyList<string> duplicateElementIds) : Exception(message)
{
    /// <summary>The element ids the document declares more than once.</summary>
    public IReadOnlyList<string> DuplicateElementIds { get; } = duplicateElementIds;
}
