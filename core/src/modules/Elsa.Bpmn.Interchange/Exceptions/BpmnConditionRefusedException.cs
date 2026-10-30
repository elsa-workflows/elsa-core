namespace Elsa.Bpmn.Interchange.Exceptions;

/// <summary>
/// Thrown when a BPMN document's sequence flows carry condition expressions Elsa cannot evaluate as the author meant,
/// per ADR "Evaluate BPMN conditions with Elsa expressions" (<c>docs/adr/2026-10-20-elsa-evaluates-bpmn-conditions.md</c>).
/// </summary>
/// <remarks>
/// <see cref="Code"/> says which rule refused the document: <see cref="BpmnErrorCodes.ImportConditionConflict"/> for a
/// flow that carries both a <c>conditionOutcome</c> and an author-written <c>conditionExpression</c>, or
/// <see cref="BpmnErrorCodes.ImportConditionLanguageUnsupported"/> for an expression written in a language Elsa does
/// not evaluate. No opt-in overrides either refusal.
/// </remarks>
public class BpmnConditionRefusedException(string code, string message, IReadOnlyList<string> flowIds) : Exception(message)
{
    /// <summary>The <see cref="BpmnErrorCodes"/> code naming the rule that refused the document.</summary>
    public string Code { get; } = code;

    /// <summary>The ids of the sequence flows the rule refused, sorted and distinct.</summary>
    public IReadOnlyList<string> FlowIds { get; } = flowIds;
}
