using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>
/// One piece of foreign content in a BPMN document — an element or attribute in a namespace that is neither BPMN's own
/// nor Elsa's — as reported over the API.
/// </summary>
[PublicAPI]
public sealed class BpmnForeignContentModel
{
    /// <summary>The id of the nearest element that carries the content, or <c>null</c> when no ancestor has one.</summary>
    public string? ElementId { get; init; }

    /// <summary><c>element</c> or <c>attribute</c>.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>The content's namespace URI.</summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>The content's local name.</summary>
    public string Name { get; init; } = string.Empty;
}
