namespace Elsa.Persistence.VNext.Runtime.Evaluation;

public record WorkflowRuntimePersistenceDecision(
    string CandidateName,
    WorkflowRuntimePersistenceDecisionKind Kind,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> RequiredEvidence);

public enum WorkflowRuntimePersistenceDecisionKind
{
    AdoptVNext,
    AdoptWithPhysicalization,
    DeferUntilBenchmarked,
    KeepSpecializedProvider
}
