namespace Elsa.Studio.Bpmn.Designers;

/// <summary>What a designer's <c>setBinding</c> returned.</summary>
/// <param name="Applied">Whether the binding was changed.</param>
/// <param name="Reason">Why not, when <paramref name="Applied"/> is false: one of <see cref="BpmnDesignerSetBindingRefusals"/>.</param>
public sealed record BpmnDesignerSetBindingResult(bool Applied, string? Reason = null);

/// <summary>The reasons a designer gives for not applying a binding.</summary>
public static class BpmnDesignerSetBindingRefusals
{
    /// <summary>The document has no element with that id.</summary>
    public const string UnknownElement = "unknown-element";

    /// <summary>The element is not one a binding can be set on, such as a gateway.</summary>
    public const string NotBindable = "not-bindable";

    /// <summary>The designer was created read-only.</summary>
    public const string ReadOnly = "read-only";
}
