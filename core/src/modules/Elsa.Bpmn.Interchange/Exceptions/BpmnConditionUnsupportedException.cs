namespace Elsa.Bpmn.Interchange.Exceptions;

/// <summary>
/// Thrown when a BPMN document's sequence flows carry a standard BPMN <c>conditionExpression</c>, which nothing in this
/// deployment evaluates yet.
/// </summary>
/// <remarks>
/// Nothing in Elsa evaluates such an expression yet, so importing the document would persist a workflow that cannot
/// route as its diagram says. Per
/// <c>docs/adr/2026-10-20-elsa-evaluates-bpmn-conditions.md</c> the document is refused instead, and no opt-in overrides
/// the refusal, until the library's condition-evaluation callback lets Elsa evaluate the expressions itself.
/// </remarks>
[Obsolete("Elsa evaluates condition expressions; import no longer refuses them. See BpmnConditionRefusedException for the conditions it still refuses.")]
public class BpmnConditionUnsupportedException(string message, IReadOnlyList<string> flowIds) : Exception(message)
{
    /// <summary>The ids of the sequence flows that carry an expression condition, sorted and distinct.</summary>
    public IReadOnlyList<string> FlowIds { get; } = flowIds;
}
