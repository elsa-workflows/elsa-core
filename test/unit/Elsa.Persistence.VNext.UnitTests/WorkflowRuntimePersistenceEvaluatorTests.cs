using Elsa.Persistence.VNext.Runtime.Evaluation;

namespace Elsa.Persistence.VNext.UnitTests;

public class WorkflowRuntimePersistenceEvaluatorTests
{
    private readonly WorkflowRuntimePersistenceEvaluator _evaluator = new();

    [Fact]
    public void Evaluator_AdoptsMetadataStoreWithProviderContractTests()
    {
        var candidate = new WorkflowRuntimePersistenceCandidate(
            "WorkflowDefinitions",
            WorkflowRuntimePersistenceArea.Management,
            WorkflowRuntimePersistenceWorkload.Metadata,
            RequiresDistributedLocking: false,
            RequiresOrderedAppend: false,
            HasRepresentativeBenchmark: false,
            HasProviderContractTests: true);

        var decision = _evaluator.Evaluate(candidate);

        Assert.Equal(WorkflowRuntimePersistenceDecisionKind.AdoptVNext, decision.Kind);
        Assert.Contains("Metadata workloads fit", decision.Reasons.Single());
    }

    [Fact]
    public void Evaluator_DefersHotDistributedLookupWithoutBenchmarkEvidence()
    {
        var candidate = new WorkflowRuntimePersistenceCandidate(
            "Bookmarks",
            WorkflowRuntimePersistenceArea.Bookmarks,
            WorkflowRuntimePersistenceWorkload.HotLookup,
            RequiresDistributedLocking: true,
            RequiresOrderedAppend: false,
            HasRepresentativeBenchmark: false,
            HasProviderContractTests: true);

        var decision = _evaluator.Evaluate(candidate);

        Assert.Equal(WorkflowRuntimePersistenceDecisionKind.DeferUntilBenchmarked, decision.Kind);
        Assert.Contains("benchmark and lock/concurrency evidence", decision.Reasons.Single());
    }

    [Fact]
    public void Evaluator_AllowsPhysicalizedHotLookupWhenEvidenceExists()
    {
        var candidate = new WorkflowRuntimePersistenceCandidate(
            "Triggers",
            WorkflowRuntimePersistenceArea.Triggers,
            WorkflowRuntimePersistenceWorkload.HotLookup,
            RequiresDistributedLocking: true,
            RequiresOrderedAppend: false,
            HasRepresentativeBenchmark: true,
            HasProviderContractTests: true);

        var decision = _evaluator.Evaluate(candidate);

        Assert.Equal(WorkflowRuntimePersistenceDecisionKind.AdoptWithPhysicalization, decision.Kind);
        Assert.Contains("physicalization policy", decision.RequiredEvidence.Single());
    }

    [Fact]
    public void Evaluator_KeepsQueueWorkloadSpecialized()
    {
        var candidate = new WorkflowRuntimePersistenceCandidate(
            "BookmarkQueue",
            WorkflowRuntimePersistenceArea.Bookmarks,
            WorkflowRuntimePersistenceWorkload.Queue,
            RequiresDistributedLocking: true,
            RequiresOrderedAppend: false,
            HasRepresentativeBenchmark: true,
            HasProviderContractTests: true);

        var decision = _evaluator.Evaluate(candidate);

        Assert.Equal(WorkflowRuntimePersistenceDecisionKind.KeepSpecializedProvider, decision.Kind);
        Assert.Contains("Queue workloads need", decision.Reasons.Single());
    }

    [Fact]
    public void Evaluator_KeepsOrderedLogSpecialized()
    {
        var candidate = new WorkflowRuntimePersistenceCandidate(
            "WorkflowExecutionLog",
            WorkflowRuntimePersistenceArea.ExecutionLogs,
            WorkflowRuntimePersistenceWorkload.AppendOnlyLog,
            RequiresDistributedLocking: false,
            RequiresOrderedAppend: true,
            HasRepresentativeBenchmark: true,
            HasProviderContractTests: true);

        var decision = _evaluator.Evaluate(candidate);

        Assert.Equal(WorkflowRuntimePersistenceDecisionKind.KeepSpecializedProvider, decision.Kind);
        Assert.Contains("ordered append", decision.Reasons.Single());
    }
}
