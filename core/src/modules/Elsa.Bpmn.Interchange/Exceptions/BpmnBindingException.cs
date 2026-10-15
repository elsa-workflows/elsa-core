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
public class BpmnBindingException(string message) : Exception(message);
