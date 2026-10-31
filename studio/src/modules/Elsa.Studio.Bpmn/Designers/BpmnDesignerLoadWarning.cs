namespace Elsa.Studio.Bpmn.Designers;

/// <summary>
/// One entry of the <c>warnings</c> a BPMN designer's <c>load</c> returns: something the designer cannot represent, or
/// will write differently, in the document it loaded.
/// </summary>
/// <param name="Kind">
/// What a save does to it: <see cref="BpmnDesignerLoadWarningKinds.Dropped"/> or
/// <see cref="BpmnDesignerLoadWarningKinds.Rewritten"/>. Kept as text, because a designer may report a kind this host
/// does not know, and the contract says to treat that kind as dropped.
/// </param>
/// <param name="Message">What the warning says, for the user.</param>
/// <param name="ElementId">The BPMN element the warning is about, when there is one.</param>
public sealed record BpmnDesignerLoadWarning(string Kind, string Message, string? ElementId = null)
{
    /// <summary>Whether a save loses this content. Any kind other than <see cref="BpmnDesignerLoadWarningKinds.Rewritten"/> counts.</summary>
    public bool IsDropped => !string.Equals(Kind, BpmnDesignerLoadWarningKinds.Rewritten, StringComparison.Ordinal);
}

/// <summary>The load warning kinds of the BPMN designer contract, version 1.</summary>
public static class BpmnDesignerLoadWarningKinds
{
    /// <summary>Content that a save would lose.</summary>
    public const string Dropped = "dropped";

    /// <summary>Content that a save would keep, but write differently.</summary>
    public const string Rewritten = "rewritten";
}
