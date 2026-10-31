namespace Elsa.Persistence.VNext.Runtime.Evaluation;

public class WorkflowRuntimePersistenceEvaluator
{
    public WorkflowRuntimePersistenceDecision Evaluate(WorkflowRuntimePersistenceCandidate candidate)
    {
        if (candidate.RequiresOrderedAppend)
        {
            return Decide(
                candidate,
                WorkflowRuntimePersistenceDecisionKind.KeepSpecializedProvider,
                "The workload requires ordered append semantics.",
                "Provider-specific batching, retention, and replay behavior must be benchmarked before vNext can own this store.");
        }

        if (candidate.Workload == WorkflowRuntimePersistenceWorkload.Queue)
        {
            return Decide(
                candidate,
                WorkflowRuntimePersistenceDecisionKind.KeepSpecializedProvider,
                "Queue workloads need lease, retry, dead-letter, and visibility-timeout semantics.",
                "Provider-specific queue/concurrency contract tests are required before replacing the current store.");
        }

        if (candidate.Workload == WorkflowRuntimePersistenceWorkload.HotLookup && candidate.RequiresDistributedLocking)
        {
            return candidate.HasRepresentativeBenchmark && candidate.HasProviderContractTests
                ? Decide(
                    candidate,
                    WorkflowRuntimePersistenceDecisionKind.AdoptWithPhysicalization,
                    "The hot lookup has benchmark and provider contract evidence.",
                    "A physicalization policy and operational rollback plan must be attached to the store.")
                : Decide(
                    candidate,
                    WorkflowRuntimePersistenceDecisionKind.DeferUntilBenchmarked,
                    "Hot distributed lookups need benchmark and lock/concurrency evidence.",
                    "Add representative provider benchmarks and distributed-locking contract tests.");
        }

        if (!candidate.HasProviderContractTests)
        {
            return Decide(
                candidate,
                WorkflowRuntimePersistenceDecisionKind.DeferUntilBenchmarked,
                "Provider contract tests are missing.",
                "Add cross-provider save/query/concurrency tests for the store shape.");
        }

        return candidate.Workload == WorkflowRuntimePersistenceWorkload.Metadata
            ? Decide(
                candidate,
                WorkflowRuntimePersistenceDecisionKind.AdoptVNext,
                "Metadata workloads fit the portable document/index model.",
                "Keep provider contract tests in CI.")
            : Decide(
                candidate,
                WorkflowRuntimePersistenceDecisionKind.AdoptWithPhysicalization,
                "Interactive queries can use vNext if declared indexes are physicalized where needed.",
                "Add provider benchmarks for the most common filters before production rollout.");
    }

    private static WorkflowRuntimePersistenceDecision Decide(
        WorkflowRuntimePersistenceCandidate candidate,
        WorkflowRuntimePersistenceDecisionKind kind,
        string reason,
        string requiredEvidence)
    {
        return new WorkflowRuntimePersistenceDecision(candidate.Name, kind, [reason], [requiredEvidence]);
    }
}
