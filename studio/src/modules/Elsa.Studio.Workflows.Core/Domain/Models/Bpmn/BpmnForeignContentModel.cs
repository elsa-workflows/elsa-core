namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// One piece of foreign content, in a namespace only some other tool interprets, that an XML <c>PUT</c> reported as
/// dropped from the stored document.
/// </summary>
public sealed class BpmnForeignContentModel
{
    /// <summary>The id of the nearest element that carried it, or <see langword="null"/> when none did.</summary>
    public string? ElementId { get; set; }

    /// <summary><c>element</c> or <c>attribute</c>.</summary>
    public string Kind { get; set; } = default!;

    /// <summary>Its namespace URI.</summary>
    public string Namespace { get; set; } = default!;

    /// <summary>Its local name.</summary>
    public string Name { get; set; } = default!;
}
