namespace Elsa.Bpmn.Interchange.Exceptions;

/// <summary>
/// Thrown when a BPMN document's sequence flows carry a standard BPMN <c>conditionExpression</c>, which nothing in this
/// deployment evaluates yet.
/// </summary>
/// <remarks>
/// <c>Bpmn.Interchange</c> reads such a flow as unconditional and reports it only as a Degraded finding, so importing the
/// document would persist a workflow that routes differently from its diagram on every run. Per
/// <c>docs/adr/2026-10-20-elsa-evaluates-bpmn-conditions.md</c> the document is refused instead, and no opt-in overrides
/// the refusal, until the library's condition-evaluation callback lets Elsa evaluate the expressions itself.
/// </remarks>
public class BpmnConditionUnsupportedException(string message, IReadOnlyList<string> flowIds) : Exception(message)
{
    /// <summary>The ids of the sequence flows that carry an expression condition, sorted and distinct.</summary>
    public IReadOnlyList<string> FlowIds { get; } = flowIds;
}
