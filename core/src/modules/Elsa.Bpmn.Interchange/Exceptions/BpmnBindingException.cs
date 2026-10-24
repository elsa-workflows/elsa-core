namespace Elsa.Bpmn.Interchange.Exceptions;

/// <summary>
/// Thrown when a BPMN document's work bindings cannot be turned into Elsa activities.
/// </summary>
/// <remarks>
/// Every refusal this type carries is a design-time one: the document, or the <c>elsa:activityBinding</c> in it, does
/// not say enough to build a runnable graph. Refusing here rather than binding something plausible is deliberate — the
/// alternatives (an unbound task quietly dropped, a call activity dispatching nothing, a timer of zero) all produce a
/// workflow that runs to completion looking healthy while doing none of what the document says.
/// </remarks>
public class BpmnBindingException : Exception
{
    /// <summary>Creates a refusal that names no single element.</summary>
    public BpmnBindingException(string message) : base(message)
    {
    }

    /// <summary>Creates a refusal of the element <paramref name="elementId"/>'s binding.</summary>
    public BpmnBindingException(string message, string? elementId) : base(message)
    {
        ElementId = elementId;
    }

    /// <summary>The BPMN id of the element whose binding is refused, when the refusal is about one element.</summary>
    public string? ElementId { get; }
}
